using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Nezzen.Application;
using Nezzen.Core;
using Nezzen.Infrastructure;

namespace Nezzen.Scanner.Benchmarks;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
        try
        {
            var sizes = new[] { 1000, 10000, 50000 }; var repetitions = 3; int? fixturesOnly = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (i + 1 >= args.Length) throw new ArgumentException("Falta valor de argumento.");
                switch (args[i])
                {
                    case "--sizes": sizes = args[++i].Split(',').Select(int.Parse).ToArray(); break;
                    case "--repetitions": repetitions = int.Parse(args[++i]); break;
                    case "--fixtures-only": fixturesOnly = int.Parse(args[++i]); break;
                    default: throw new ArgumentException("Argumento desconocido: " + args[i]);
                }
            }
            if (sizes.Any(n => n < 100 || n > 50000) || sizes.Length > 10 || repetitions is < 1 or > 10 || fixturesOnly is < 1 or > 50000)
                throw new ArgumentOutOfRangeException(nameof(args), "Tamaños 100..50000; repeticiones 1..10; fixtures 1..50000.");
            if (fixturesOnly is int fixtureCount)
            {
                var root = CreateRoot();
                try { Generate(root, fixtureCount, stop.Token); }
                catch { Directory.Delete(root, true); throw; }
                Console.WriteLine(JsonSerializer.Serialize(new { Root = root, Music = Path.Combine(root, "music"), Files = fixtureCount, BytesPerFixture = 172, RetainedForManualValidation = true }));
                return 0;
            }
            // Exercise the complete pipeline/JIT before recorded cases. OS caches are not flushed.
            Console.Error.WriteLine("Warmup: 100 files (not included).");
            await RunCaseAsync(100, 0, stop.Token);
            var cases = new List<CaseResult>();
            foreach (var size in sizes)
                for (var repeat = 1; repeat <= repetitions; repeat++)
                {
                    Console.Error.WriteLine($"Benchmark: {size} files, repetition {repeat}/{repetitions}.");
                    cases.Add(await RunCaseAsync(size, repeat, stop.Token));
                }
            var process = Process.GetCurrentProcess();
            var report = new
            {
                SchemaVersion = 1, Utc = DateTimeOffset.UtcNow, OS = RuntimeInformation.OSDescription,
                Runtime = RuntimeInformation.FrameworkDescription, Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                LogicalProcessors = Environment.ProcessorCount, StopwatchFrequency = Stopwatch.Frequency,
                MemoryLimit = ReadIfPresent("/sys/fs/cgroup/memory.max"), CpuLimit = ReadIfPresent("/sys/fs/cgroup/cpu.max"),
                CpuModel = ReadIfPresent("/proc/cpuinfo")?.Split('\n').FirstOrDefault(l => l.StartsWith("model name", StringComparison.Ordinal)),
                TemporaryDirectory = Path.GetTempPath(), FixtureBytes = 172, Repetitions = repetitions,
                Notes = "Warmup 100 files; filesystem cache not flushed; sequential cases, fresh corpus/DB each repetition. Memory sampled every ~25ms, absolute process/managed memory, not exact peaks. Allocation is process-wide including sampler. No audio decoding or GUI.",
                Cases = cases, ProcessPeakWorkingSetBytes = process.PeakWorkingSet64
            };
            Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return e is OperationCanceledException ? 130 : 1; }
    }
    private static string? ReadIfPresent(string path) => File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nezzen-benchmark-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, ".nezzen-synthetic-only"), "Owned synthetic corpus; no personal music."); return root;
    }
    private static string FixturePath(string root, int index) => Path.Combine(root, "music", $"group-{index / 100:D4}", $"track-{index:D6}.wav");
    private static void Generate(string root, int count, CancellationToken token)
    {
        for (var i = 0; i < count; i++) { token.ThrowIfCancellationRequested(); WriteWave(FixturePath(root, i), i); }
    }
    private static void WriteWave(string path, int identity)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(file, Encoding.ASCII);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(164); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(8000); writer.Write(16000); writer.Write((short)2); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(128); writer.Write(identity); writer.Write(new byte[124]);
    }
    private static async Task<CaseResult> RunCaseAsync(int size, int repeat, CancellationToken token)
    {
        var root = CreateRoot();
        try
        {
            var setup = Stopwatch.StartNew(); Generate(root, size, token);
            var repository = new SqliteLibraryRepository(Path.Combine(root, "library.db")); await repository.InitializeAsync(token);
            var folder = await new MusicFolderService(repository).RegisterAsync(Path.Combine(root, "music"), token);
            var metadata = new CountMetadata(); var fingerprints = new CountFingerprints();
            var scanner = new IncrementalLibraryScanner(repository, new PhysicalMusicFileSystem(), metadata, fingerprints);
            setup.Stop();
            var stages = new List<StageResult>();
            stages.Add(await StageAsync("first_import", scanner, folder, metadata, fingerprints, size, size, 0, 0, 0, 0, token));
            stages.Add(await StageAsync("unchanged", scanner, folder, metadata, fingerprints, size, 0, 0, size, 0, 0, token));
            var changed = size / 100;
            var changeClock = Stopwatch.StartNew();
            for (var i = 0; i < changed; i++)
            {
                token.ThrowIfCancellationRequested();
                var path = FixturePath(root, i); var oldTime = File.GetLastWriteTimeUtc(path);
                WriteWave(path, 1000000 + i); File.SetLastWriteTimeUtc(path, oldTime.AddDays(1));
                var target = Path.Combine(root, "music", "moved", $"track-{i:D6}.wav"); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Move(FixturePath(root, changed + i), target);
                File.Delete(FixturePath(root, 2 * changed + i));
                WriteWave(FixturePath(root, size + i), size + i);
            }
            changeClock.Stop();
            stages.Add(await StageAsync("controlled_changes", scanner, folder, metadata, fingerprints, size, changed, changed, size - 3 * changed, changed, changed, token));
            var tracks = await repository.GetTracksAsync(folder.Id, token);
            Require(tracks.Count == size + changed && tracks.Count(t => t.Status == FileStatus.Missing) == changed, "Final database counts incorrect.");
            var bytes = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Sum(p => new FileInfo(p).Length);
            return new(size, repeat, setup.Elapsed.TotalSeconds, changeClock.Elapsed.TotalSeconds, bytes, new FileInfo(Path.Combine(root, "library.db")).Length, stages);
        }
        finally
        {
            // Only deletes the unique root created above. No caller-supplied filesystem paths.
            Directory.Delete(root, true);
        }
    }
    private static async Task<StageResult> StageAsync(string stage, IncrementalLibraryScanner scanner, MusicFolder folder,
        CountMetadata metadata, CountFingerprints fingerprints, int size, int added, int modified, int unchanged, int moved, int missing, CancellationToken token)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        metadata.Reads = 0; fingerprints.Reads = 0; fingerprints.Bytes = 0;
        var progress = new CaptureProgress();
        ScanPreview? preview = null;
        var analysis = await MeasureAsync(async () => { preview = await scanner.AnalyzeAsync([folder], progress, token); });
        var p = preview ?? throw new InvalidOperationException("No preview."); var r = p.Result;
        Require(r.New == added && r.Modified == modified && r.Unchanged == unchanged && r.Moved == moved && r.Missing == missing && r.Errors == 0 && r.Unavailable == 0 && r.PossibleMoves == 0, "Unexpected scan classification.");
        Require(progress.Processed == size && progress.Found == size, "Incorrect progress counts.");
        var metadataReads = metadata.Reads; var analyzeHashes = fingerprints.Reads; var analyzeHashBytes = fingerprints.Bytes;
        Require(metadataReads == added + modified + moved && analyzeHashes == added + modified + moved, $"Unexpected reads in {stage}: metadata={metadataReads}, hashes={analyzeHashes}, expected={added + modified + moved}.");
        fingerprints.Reads = 0; fingerprints.Bytes = 0;
        var apply = await MeasureAsync(async () => { await scanner.ApplyAsync(p.Id, cancellationToken: token); });
        Require(fingerprints.Reads == added + modified + moved, "Unexpected apply revalidation hash count.");
        return new(stage, progress.Processed, analysis, apply, size / analysis.Seconds, size / (analysis.Seconds + apply.Seconds),
            metadataReads, analyzeHashes, fingerprints.Reads, analyzeHashBytes, fingerprints.Bytes, added, modified, unchanged, moved, missing);
    }
    private static async Task<Measurement> MeasureAsync(Func<Task> action)
    {
        using var process = Process.GetCurrentProcess(); process.Refresh();
        var startWorkingSet = process.WorkingSet64; var startManaged = GC.GetTotalMemory(false);
        long peakWorkingSet = startWorkingSet, peakManaged = startManaged;
        using var sampleStop = new CancellationTokenSource();
        void Sample() { process.Refresh(); peakWorkingSet = Math.Max(peakWorkingSet, process.WorkingSet64); peakManaged = Math.Max(peakManaged, GC.GetTotalMemory(false)); }
        var sampler = Task.Run(async () =>
        {
            try { while (true) { await Task.Delay(25, sampleStop.Token); Sample(); } }
            catch (OperationCanceledException) { }
        });
        var allocated = GC.GetTotalAllocatedBytes(true); var clock = Stopwatch.StartNew();
        try { await action(); }
        finally { clock.Stop(); sampleStop.Cancel(); await sampler; Sample(); }
        return new(clock.Elapsed.TotalSeconds, startWorkingSet, process.WorkingSet64, peakWorkingSet,
            startManaged, GC.GetTotalMemory(false), peakManaged, GC.GetTotalAllocatedBytes(true) - allocated);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class CaptureProgress : IProgress<ScanProgress>
    {
        public long Processed, Found;
        public void Report(ScanProgress value) { Processed = value.Processed; Found = value.Found; }
    }
    private sealed class CountMetadata : IFileMetadataReader
    {
        private readonly TagLibMetadataReader reader = new(); public int Reads;
        public Task<TrackMetadata> ReadAsync(string path, CancellationToken cancellationToken = default) { Reads++; return reader.ReadAsync(path, cancellationToken); }
    }
    private sealed class CountFingerprints : IFileFingerprinter
    {
        private readonly StreamingFileFingerprinter reader = new(); public int Reads; public long Bytes;
        public async Task<FileFingerprint> ReadAsync(string path, bool includeContentHash, CancellationToken cancellationToken = default)
        {
            var result = await reader.ReadAsync(path, includeContentHash, cancellationToken);
            if (includeContentHash) { Reads++; Bytes += result.Size; } return result;
        }
    }
    private sealed record Measurement(double Seconds, long StartWorkingSetBytes, long EndWorkingSetBytes, long SampledPeakWorkingSetBytes,
        long StartManagedBytes, long EndManagedBytes, long SampledPeakManagedBytes, long AllocatedBytes);
    private sealed record StageResult(string Stage, long Processed, Measurement Analyze, Measurement Apply,
        double AnalyzeFilesPerSecond, double EndToEndFilesPerSecond, int MetadataReads, int AnalyzeHashReads, int ApplyHashReads,
        long AnalyzeHashBytes, long ApplyHashBytes, int New, int Modified, int Unchanged, int Moved, int Missing);
    private sealed record CaseResult(int Size, int Repetition, double SetupSeconds, double MutationSeconds, long TotalLogicalBytes, long DatabaseBytes, List<StageResult> Stages);
}
