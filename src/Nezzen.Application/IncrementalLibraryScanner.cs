using Nezzen.Core;
using System.Diagnostics;

namespace Nezzen.Application;

public sealed class IncrementalLibraryScanner(
    ILibraryRepository repository,
    IMusicFileSystem fileSystem,
    IFileMetadataReader metadataReader,
    IFileFingerprinter fingerprinter) : ILibraryScanner
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private PendingScan? pending;
    private sealed record PendingScan(ScanPreview Preview, long Revision, DateTimeOffset StartedUtc,
        MusicFolder[] Folders, FolderObservation[] Observations, Track[] Updates, HashSet<string> RevalidateHashes);

    // Legacy contract is analysis only; never applies changes implicitly.
    public async Task<ScanResult> ScanAsync(IReadOnlyList<MusicFolder> folders, IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default) => (await AnalyzeAsync(folders, progress, cancellationToken)).Result;

    public async Task<ScanPreview> AnalyzeAsync(IReadOnlyList<MusicFolder> folders, IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var requestedIds = folders.Select(f => f.Id).Distinct().ToArray();
        await gate.WaitAsync(cancellationToken);
        try
        {
            pending = null;
            return await Task.Run(() => AnalyzeCoreAsync(requestedIds, progress, cancellationToken), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            progress?.Report(new ScanProgress(0, null) { State = "Cancelado" });
            throw;
        }
        finally { gate.Release(); }
    }

    private async Task<ScanPreview> AnalyzeCoreAsync(Guid[] requestedIds, IProgress<ScanProgress>? progress, CancellationToken token)
    {
        var started = DateTimeOffset.UtcNow;
        var snapshot = await repository.GetSnapshotAsync(token);
        if (requestedIds.Any(id => snapshot.Folders.All(f => f.Id != id))) throw new ArgumentException("Carpeta no registrada.");
        var folders = snapshot.Folders.Where(f => f.Enabled && requestedIds.Contains(f.Id)).ToArray();
        for (var i = 0; i < folders.Length; i++)
            for (var j = 0; j < i; j++)
                if (LibraryPath.Contains(folders[i].Path, folders[j].Path) || LibraryPath.Contains(folders[j].Path, folders[i].Path))
                    throw new ArgumentException("Las raíces registradas se solapan.");
        var selected = folders.Select(f => f.Id).ToHashSet();
        var tracks = snapshot.Tracks.Where(t => selected.Contains(t.FolderId)).ToArray();
        var byPath = tracks.ToDictionary(t => LibraryPath.Key(t.Path));
        var observations = new List<FolderObservation>();
        var entries = new List<ScanEntry>();
        var issues = new List<ScanIssue>();
        var updates = new Dictionary<Guid, Track>();
        var additions = new List<Track>();
        var revalidateHashes = new HashSet<string>();
        var missing = new List<Track>();
        long found = 0, processed = 0;
        var verified = DateTimeOffset.UtcNow;
        var progressClock = Stopwatch.StartNew();
        var tracksByFolder = tracks.ToLookup(t => t.FolderId);

        foreach (var folder in folders)
        {
            token.ThrowIfCancellationRequested();
            progress?.Report(new ScanProgress(processed, folder.Path) { Found = found, Errors = issues.Count, State = "Descubriendo" });
            var observation = await DiscoverSafeAsync(folder, token);
            observations.Add(observation); issues.AddRange(observation.Issues);
            foreach (var issue in observation.Issues) entries.Add(new(issue.Path, FileChange.Error, Message: issue.Message));
            found += observation.Files.Count;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in observation.Files)
            {
                token.ThrowIfCancellationRequested();
                var key = LibraryPath.Key(file.Path);
                if (!seen.Add(key)) continue;
                byPath.TryGetValue(key, out var previous);
                try
                {
                    // Cheap observation avoids opening unchanged files at all.
                    if (previous is not null && previous.Status == FileStatus.Available && !previous.MetadataReadFailed && FileChangeClassifier.Classify(previous.Fingerprint, file.Fingerprint, true) == FileChange.Unchanged)
                    {
                        updates[previous.Id] = previous with { LastVerifiedUtc = verified, Status = FileStatus.Available };
                        entries.Add(new(file.Path, FileChange.Unchanged, previous.Id));
                    }
                    else
                    {
                        var fingerprint = await fingerprinter.ReadAsync(file.Path, true, token);
                        if (!SameStat(fingerprint, file.Fingerprint)) throw new IOException("El archivo cambió durante el análisis.");
                        TrackMetadata metadata;
                        var metadataFailed = false;
                        try { metadata = await metadataReader.ReadAsync(file.Path, token); }
                        catch (Exception e) when (IsFileError(e))
                        {
                            metadataFailed = true;
                            var message = "Metadatos no disponibles: " + e.Message;
                            issues.Add(new(file.Path, message)); entries.Add(new(file.Path, FileChange.Error, previous?.Id, Message: message));
                            metadata = previous is not null ? MetadataFrom(previous) : new(FallbackTitle(file.Path), null, null, TimeSpan.Zero, Path.GetExtension(file.Path).TrimStart('.'));
                        }
                        var after = await fingerprinter.ReadAsync(file.Path, false, token);
                        if (!SameStat(after, fingerprint)) throw new IOException("El archivo cambió al leer metadatos.");
                        var track = WithMetadata(previous ?? new Track(Guid.NewGuid(), folder.Id, file.Path, metadata.Title, fingerprint), metadata) with
                        { Fingerprint = fingerprint, LastVerifiedUtc = verified, Status = FileStatus.Available, Path = file.Path, MetadataReadFailed = metadataFailed };
                        revalidateHashes.Add(key);
                        if (previous is null) additions.Add(track);
                        else { updates[track.Id] = track; entries.Add(new(file.Path, FileChangeClassifier.Classify(previous.Fingerprint, fingerprint, true), track.Id)); }
                    }
                }
                catch (Exception e) when (IsFileError(e))
                {
                    issues.Add(new(file.Path, e.Message)); entries.Add(new(file.Path, FileChange.Error, previous?.Id, Message: e.Message));
                    if (previous is not null)
                    {
                        updates[previous.Id] = previous with { Status = FileStatus.Unavailable };
                        entries.Add(new(file.Path, FileChange.Unavailable, previous.Id));
                    }
                }
                processed++;
                if (processed == 1 || progressClock.ElapsedMilliseconds >= 100)
                {
                    progress?.Report(new ScanProgress(processed, file.Path) { Found = found, Errors = issues.Count });
                    progressClock.Restart();
                }
            }
            foreach (var track in tracksByFolder[folder.Id].Where(t => !seen.Contains(LibraryPath.Key(t.Path))))
            {
                if (observation.Complete) missing.Add(track);
                else
                {
                    updates[track.Id] = track with { Status = FileStatus.Unavailable };
                    entries.Add(new(track.Path, FileChange.Unavailable, track.Id));
                }
            }
        }

        var missingHashes = missing.Where(t => t.Fingerprint.ContentHash is not null)
            .GroupBy(HashKey).ToDictionary(g => g.Key, g => g.ToArray());
        var newHashes = additions.GroupBy(HashKey).ToDictionary(g => g.Key, g => g.ToArray());
        var knownHashes = snapshot.Tracks.Where(t => t.Fingerprint.ContentHash is not null).GroupBy(HashKey).ToDictionary(g => g.Key, g => g.Count());
        var movedIds = new HashSet<Guid>();
        var candidates = 0;
        foreach (var addition in additions)
        {
            token.ThrowIfCancellationRequested();
            missingHashes.TryGetValue(HashKey(addition), out var matches);
            if (matches is { Length: 1 } && newHashes[HashKey(addition)].Length == 1 && knownHashes[HashKey(addition)] == 1)
            {
                var old = matches[0]; movedIds.Add(old.Id);
                // Preserve all preferences and incorporation date from the original identity.
                var moved = WithMetadata(old, MetadataFrom(addition)) with
                { Path = addition.Path, FolderId = addition.FolderId, Fingerprint = addition.Fingerprint, LastVerifiedUtc = verified, Status = FileStatus.Available, MetadataReadFailed = addition.MetadataReadFailed };
                updates[old.Id] = moved;
                entries.Add(new(addition.Path, FileChange.Moved, old.Id, old.Path, MatchConfidence.ContentHash));
            }
            else
            {
                var ambiguous = matches is { Length: > 0 };
                if (ambiguous) candidates++;
                updates[addition.Id] = addition;
                entries.Add(new(addition.Path, FileChange.New, addition.Id, Confidence: ambiguous ? MatchConfidence.ContentHash : MatchConfidence.None,
                    Message: ambiguous ? "Coincidencia de contenido ambigua; se conserva una identidad nueva." : null));
            }
        }
        foreach (var track in missing.Where(t => !movedIds.Contains(t.Id)))
        {
            updates[track.Id] = track with { Status = FileStatus.Missing, LastVerifiedUtc = verified };
            entries.Add(new(track.Path, FileChange.Missing, track.Id));
        }
        token.ThrowIfCancellationRequested();
        var id = Guid.NewGuid();
        var result = new ScanResult(id, entries.Count(e => e.Change == FileChange.New), entries.Count(e => e.Change == FileChange.Modified),
            entries.Count(e => e.Change == FileChange.Unchanged), entries.Count(e => e.Change == FileChange.Missing),
            entries.Count(e => e.Change == FileChange.Unavailable), candidates, 0, issues.AsReadOnly())
        { Moved = entries.Count(e => e.Change == FileChange.Moved) };
        var preview = new ScanPreview(id, result, entries.AsReadOnly());
        progress?.Report(new ScanProgress(processed, null) { Found = found, Errors = issues.Count, State = "Vista previa lista" });
        token.ThrowIfCancellationRequested();
        pending = new(preview, snapshot.Revision, started, folders, observations.ToArray(), updates.Values.ToArray(), revalidateHashes);
        return preview;
    }

    public async Task<ScanResult> ApplyAsync(Guid previewId, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var scan = pending;
            if (scan is null || scan.Preview.Id != previewId) throw new StaleScanPreviewException("Vista previa inexistente o ya reemplazada/aplicada.");
            return await Task.Run(async () =>
            {
                var token = cancellationToken;
                if ((await repository.GetSnapshotAsync(token)).Revision != scan.Revision) throw new StaleScanPreviewException("La biblioteca cambió; vuelve a analizar.");
                progress?.Report(new ScanProgress(0, null) { State = "Validando vista previa" });
                foreach (var folder in scan.Folders)
                {
                    var expected = scan.Observations.Single(o => o.FolderId == folder.Id);
                    var actual = await DiscoverSafeAsync(folder, token);
                    if (expected.Complete != actual.Complete || !SameFiles(expected.Files, actual.Files))
                        throw new StaleScanPreviewException("La carpeta cambió; vuelve a analizar.");
                }
                // Only records whose content was read during analysis are hashed again.
                var changedPaths = scan.RevalidateHashes;
                foreach (var track in scan.Updates.Where(t => t.Status == FileStatus.Available && changedPaths.Contains(LibraryPath.Key(t.Path))))
                {
                    token.ThrowIfCancellationRequested();
                    FileFingerprint actual;
                    try { actual = await fingerprinter.ReadAsync(track.Path, true, token); }
                    catch (Exception e) when (IsFileError(e)) { throw new StaleScanPreviewException("El archivo ya no se puede verificar: " + e.Message); }
                    if (actual != track.Fingerprint) throw new StaleScanPreviewException("El contenido cambió; vuelve a analizar.");
                }
                var session = new ScanSession(scan.Preview.Id, scan.StartedUtc, DateTimeOffset.UtcNow, ScanState.Completed);
                await repository.CommitScanAsync(scan.Revision, scan.Updates, session, scan.Preview.Result, scan.Preview.Entries, token);
                pending = null;
                progress?.Report(new ScanProgress(scan.Preview.Entries.Count, null) { State = "Aplicado", Errors = scan.Preview.Result.Errors });
                return scan.Preview.Result;
            }, cancellationToken);
        }
        finally { gate.Release(); }
    }

    private async Task<FolderObservation> DiscoverSafeAsync(MusicFolder folder, CancellationToken token)
    {
        try { return await fileSystem.DiscoverAsync(folder, token); }
        catch (Exception e) when (IsFileError(e)) { return new(folder.Id, false, [], [new ScanIssue(folder.Path, e.Message)]); }
    }
    private static string FallbackTitle(string path) => string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(path)) ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
    private static bool SameStat(FileFingerprint a, FileFingerprint b) => a.Size == b.Size && a.LastWriteUtc == b.LastWriteUtc;
    private static string HashKey(Track track) => track.Fingerprint.Size + ":" + track.Fingerprint.ContentHash;
    private static bool SameFiles(IReadOnlyList<FileObservation> a, IReadOnlyList<FileObservation> b)
    {
        if (a.Count != b.Count) return false;
        var index = a.ToDictionary(f => LibraryPath.Key(f.Path));
        return b.All(f => index.TryGetValue(LibraryPath.Key(f.Path), out var expected) && SameStat(expected.Fingerprint, f.Fingerprint));
    }
    private static bool IsFileError(Exception e) => e is IOException or UnauthorizedAccessException or NotSupportedException or InvalidDataException;
    private static Track WithMetadata(Track track, TrackMetadata m) => track with
    {
        Title = string.IsNullOrWhiteSpace(m.Title) ? FallbackTitle(track.Path) : m.Title,
        Artist = m.Artist,
        AlbumArtist = m.AlbumArtist,
        Album = m.Album,
        Duration = m.Duration,
        Format = m.Format,
        TrackNumber = m.TrackNumber,
        DiscNumber = m.DiscNumber,
        Year = m.Year,
        Genre = m.Genre,
        Bitrate = m.Bitrate,
        SampleRate = m.SampleRate
    };
    private static TrackMetadata MetadataFrom(Track t) => new(t.Title, t.Artist, t.Album, t.Duration, t.Format)
    { AlbumArtist = t.AlbumArtist, TrackNumber = t.TrackNumber, DiscNumber = t.DiscNumber, Year = t.Year, Genre = t.Genre, Bitrate = t.Bitrate, SampleRate = t.SampleRate };
}
