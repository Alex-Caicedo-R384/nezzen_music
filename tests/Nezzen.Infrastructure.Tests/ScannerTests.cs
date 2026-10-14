using System.Text;
using Nezzen.Application;
using Nezzen.Core;
using Nezzen.Infrastructure;
using Xunit;

namespace Nezzen.Infrastructure.Tests;

public sealed class ScannerTests : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "nezzen-scan-" + Guid.NewGuid());
    private string Music => Path.Combine(root, "music");
    private SqliteLibraryRepository repository = null!;
    private MusicFolder folder = null!;
    private CountingMetadata metadata = null!;
    private CountingFingerprinter fingerprints = null!;
    private IncrementalLibraryScanner scanner = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Music);
        repository = new SqliteLibraryRepository(Path.Combine(root, "library.db"));
        await repository.InitializeAsync();
        folder = await new MusicFolderService(repository).RegisterAsync(Music);
        metadata = new CountingMetadata(); fingerprints = new CountingFingerprinter();
        scanner = new IncrementalLibraryScanner(repository, new PhysicalMusicFileSystem(), metadata, fingerprints);
    }
    public Task DisposeAsync() { Directory.Delete(root, true); return Task.CompletedTask; }
    private Task<ScanPreview> Analyze() => scanner.AnalyzeAsync([folder]);
    private async Task Import() { var p = await Analyze(); await scanner.ApplyAsync(p.Id); }
    private async Task<Track> OnlyTrack() => Assert.Single(await repository.GetTracksAsync(folder.Id));
    private string Wave(string name = "a.wav", byte value = 1)
    {
        var path = Path.Combine(Music, name); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = File.Create(path); using var writer = new BinaryWriter(stream, Encoding.ASCII);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + 1600); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(8000); writer.Write(16000); writer.Write((short)2); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(1600); writer.Write(Enumerable.Repeat(value, 1600).ToArray()); return path;
    }
    [Fact]
    public async Task PreviewDoesNotMutateLibraryOrHistory()
    {
        var path = Wave(); var bytes = await File.ReadAllBytesAsync(path); var p = await Analyze();
        Assert.Equal(1, p.Result.New); Assert.Empty(await repository.GetTracksAsync(folder.Id)); Assert.Empty(await repository.GetScanHistoryAsync());
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        await scanner.ApplyAsync(p.Id); var t = await OnlyTrack(); Assert.Equal(path, t.Path); Assert.NotNull(t.LastVerifiedUtc);
        Assert.Equal(8000, t.SampleRate); Assert.True(t.Duration > TimeSpan.Zero); Assert.Equal("a", t.Title);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path)); Assert.Single(await repository.GetScanHistoryAsync());
    }
    [Fact]
    public async Task SecondScanSkipsMetadataAndHash()
    {
        Wave(); await Import(); metadata.Reads = 0; fingerprints.Hashes = 0;
        var p = await Analyze(); Assert.Equal(1, p.Result.Unchanged); Assert.Equal(0, metadata.Reads); Assert.Equal(0, fingerprints.Hashes);
        await scanner.ApplyAsync(p.Id); Assert.Single(await repository.GetTracksAsync(folder.Id));
    }
    [Fact]
    public async Task NewFileDoesNotDuplicateExisting()
    {
        Wave(); await Import(); Wave("b.wav", 2); var p = await Analyze(); Assert.Equal(1, p.Result.New); Assert.Equal(1, p.Result.Unchanged);
        await scanner.ApplyAsync(p.Id); Assert.Equal(2, (await repository.GetTracksAsync(folder.Id)).Count);
    }
    [Fact]
    public async Task ModifiedPreservesIdentityAndPreferences()
    {
        Wave(); await Import(); var original = await OnlyTrack(); await repository.SaveTrackAsync(original with { PlaybackSpeed = 1.25 });
        var path = Wave(value: 3); File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
        var p = await Analyze(); Assert.Equal(1, p.Result.Modified); await scanner.ApplyAsync(p.Id);
        var actual = await OnlyTrack(); Assert.Equal(original.Id, actual.Id); Assert.Equal(1.25, actual.PlaybackSpeed);
        Assert.NotEqual(original.Fingerprint.ContentHash, actual.Fingerprint.ContentHash);
    }
    [Theory]
    [InlineData("renamed.wav")]
    [InlineData("sub/moved.wav")]
    public async Task RenameAndMoveKeepId(string destination)
    {
        var path = Wave(); await Import(); var original = await OnlyTrack();
        var target = Path.Combine(Music, destination); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Move(path, target);
        var p = await Analyze(); Assert.Equal(1, p.Result.Moved); Assert.Equal(0, p.Result.New); Assert.Equal(0, p.Result.Missing);
        Assert.Equal(MatchConfidence.ContentHash, Assert.Single(p.Entries).Confidence);
        await scanner.ApplyAsync(p.Id); var actual = await OnlyTrack(); Assert.Equal(original.Id, actual.Id); Assert.Equal(target, actual.Path);
    }
    [Fact]
    public async Task MissingRemainsInDatabase()
    {
        var path = Wave(); await Import(); File.Delete(path); var p = await Analyze(); Assert.Equal(1, p.Result.Missing);
        await scanner.ApplyAsync(p.Id); Assert.Equal(FileStatus.Missing, (await OnlyTrack()).Status);
    }
    [Fact]
    public async Task DisconnectedFolderPreservesTracks()
    {
        Wave(); await Import(); Directory.Move(Music, Music + "-offline"); var p = await Analyze();
        Assert.Equal(0, p.Result.Missing); Assert.Equal(1, p.Result.Unavailable); await scanner.ApplyAsync(p.Id);
        Assert.Equal(FileStatus.Unavailable, (await OnlyTrack()).Status);
    }
    [Fact]
    public async Task CorruptFileHasFallbackAndDoesNotStopOthers()
    {
        Wave(); await File.WriteAllTextAsync(Path.Combine(Music, "broken.mp3"), "invalid audio");
        var p = await Analyze(); Assert.True(p.Result.Errors > 0); Assert.Equal(2, p.Result.New); await scanner.ApplyAsync(p.Id);
        Assert.Equal("broken", (await repository.GetTracksAsync(folder.Id)).Single(t => t.Path.EndsWith("broken.mp3")).Title);
    }
    [Fact]
    public async Task UnreadableFileDoesNotHideOtherFilesOrReplaceExisting()
    {
        var path = Wave(); await Import(); Wave("b.wav", 2); File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(1)); fingerprints.DenyPath = path;
        var p = await Analyze(); Assert.True(p.Result.Errors > 0); Assert.Equal(0, p.Result.Missing); Assert.Equal(1, p.Result.Unavailable); await scanner.ApplyAsync(p.Id);
        Assert.Equal(2, (await repository.GetTracksAsync(folder.Id)).Count);
    }
    [Fact]
    public async Task AmbiguousHashesDoNotMergeIdentities()
    {
        var a = Wave(); var b = Wave("b.wav"); await Import(); File.Delete(a); File.Delete(b); Wave("c.wav");
        var p = await Analyze(); Assert.Equal(0, p.Result.Moved); Assert.Equal(1, p.Result.New); Assert.Equal(2, p.Result.Missing);
        Assert.True(p.Result.PossibleMoves > 0); await scanner.ApplyAsync(p.Id); Assert.Equal(3, (await repository.GetTracksAsync(folder.Id)).Count);
    }
    [Fact]
    public async Task PreviewRejectsChangedFilesystem()
    {
        var path = Wave(); var p = await Analyze(); File.AppendAllText(path, "changed");
        await Assert.ThrowsAsync<StaleScanPreviewException>(() => scanner.ApplyAsync(p.Id));
        Assert.Empty(await repository.GetTracksAsync(folder.Id)); Assert.Empty(await repository.GetScanHistoryAsync());
    }
    [Fact]
    public async Task PreviewRejectsNewFileBeforeApply()
    {
        Wave(); var p = await Analyze(); Wave("later.wav");
        await Assert.ThrowsAsync<StaleScanPreviewException>(() => scanner.ApplyAsync(p.Id)); Assert.Empty(await repository.GetTracksAsync(folder.Id));
    }
    [Fact]
    public async Task PreviewRejectsContentChangeWithSameSizeAndTime()
    {
        var path = Wave(); var p = await Analyze(); var time = File.GetLastWriteTimeUtc(path); Wave(value: 4); File.SetLastWriteTimeUtc(path, time);
        await Assert.ThrowsAsync<StaleScanPreviewException>(() => scanner.ApplyAsync(p.Id));
    }
    [Fact]
    public async Task PreviewRejectsLibraryChangesAndCannotBeAppliedTwice()
    {
        Wave(); var p = await Analyze(); await repository.SaveFolderAsync(folder with { Enabled = false });
        await Assert.ThrowsAsync<StaleScanPreviewException>(() => scanner.ApplyAsync(p.Id)); Assert.Empty(await repository.GetTracksAsync(folder.Id));
        await repository.SaveFolderAsync(folder); p = await Analyze(); await scanner.ApplyAsync(p.Id);
        await Assert.ThrowsAsync<StaleScanPreviewException>(() => scanner.ApplyAsync(p.Id));
    }
    [Fact]
    public async Task CancellationLeavesLibraryUntouched()
    {
        Wave(); using var cts = new CancellationTokenSource();
        var progress = new ImmediateProgress(p => { if (p.Processed > 0) cts.Cancel(); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scanner.AnalyzeAsync([folder], progress, cts.Token));
        Assert.Empty(await repository.GetTracksAsync(folder.Id)); Assert.Empty(await repository.GetScanHistoryAsync());
    }
    [Fact]
    public async Task CancelledApplyDoesNotWrite()
    {
        Wave(); var p = await Analyze(); using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scanner.ApplyAsync(p.Id, cancellationToken: cts.Token)); Assert.Empty(await repository.GetTracksAsync(folder.Id));
    }
    [Fact]
    public async Task HistoryAndResultsSurviveReopen()
    {
        Wave(); await Import(); var reopened = new SqliteLibraryRepository(Path.Combine(root, "library.db")); await reopened.InitializeAsync();
        var session = Assert.Single(await reopened.GetScanHistoryAsync()); Assert.Equal(ScanState.Completed, session.State);
        var result = await reopened.GetScanResultAsync(session.Id); Assert.NotNull(result); Assert.Equal(1, result.New); Assert.Single(await reopened.GetScanEntriesAsync(session.Id));
    }
    [Fact]
    public async Task RegistrationIsIdempotentAndRejectsOverlaps()
    {
        var service = new MusicFolderService(repository); var duplicate = await service.RegisterAsync(Music + Path.DirectorySeparatorChar); Assert.Equal(folder.Id, duplicate.Id);
        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(Path.Combine(Music, "nested")));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(root)); Assert.Single(await service.GetAsync());
        await service.SetEnabledAsync(folder.Id, false); Assert.False(Assert.Single(await service.GetAsync()).Enabled);
        Assert.Empty((await Analyze()).Entries);
    }
    [Fact]
    public async Task SymlinksDoNotCreateCyclesOrDuplicateFiles()
    {
        Wave(); Directory.CreateSymbolicLink(Path.Combine(Music, "loop"), Music);
        var p = await Analyze(); Assert.Equal(1, p.Result.New); Assert.True(p.Result.Errors > 0);
        await Assert.ThrowsAsync<ArgumentException>(() => new MusicFolderService(repository).RegisterAsync(Path.Combine(Music, "loop")));
    }
    [Fact]
    public async Task PartialEnumerationDoesNotMarkAbsentTracksMissing()
    {
        Wave(); await Import();
        var partial = new PartialFileSystem(); var other = new IncrementalLibraryScanner(repository, partial, metadata, fingerprints);
        var p = await other.AnalyzeAsync([folder]); Assert.Equal(0, p.Result.Missing); Assert.Equal(1, p.Result.Unavailable);
    }
    [Fact]
    public async Task DuplicateContentWithOriginalPresentIsNewFile()
    {
        var path = Wave(); await Import(); File.Copy(path, Path.Combine(Music, "copy.wav"));
        var p = await Analyze(); Assert.Equal(1, p.Result.New); Assert.Equal(0, p.Result.Moved); await scanner.ApplyAsync(p.Id);
        Assert.Equal(2, (await repository.GetTracksAsync(folder.Id)).Count);
    }
    [Fact]
    public async Task KnownDuplicateContentMakesMovementAmbiguous()
    {
        var path = Wave(); Wave("copy.wav"); await Import(); File.Move(path, Path.Combine(Music, "renamed.wav"));
        var p = await Analyze(); Assert.Equal(0, p.Result.Moved); Assert.True(p.Result.PossibleMoves > 0);
    }
    [Fact]
    public async Task CrossRootMovePreservesIdentityAndChangesOwner()
    {
        var path = Wave(); await Import(); var original = await OnlyTrack();
        var secondPath = Path.Combine(root, "other"); Directory.CreateDirectory(secondPath);
        var other = await new MusicFolderService(repository).RegisterAsync(secondPath);
        File.Move(path, Path.Combine(secondPath, "moved.wav"));
        var p = await scanner.AnalyzeAsync([folder, other]); Assert.Equal(1, p.Result.Moved); await scanner.ApplyAsync(p.Id);
        Assert.Empty(await repository.GetTracksAsync(folder.Id)); var moved = Assert.Single(await repository.GetTracksAsync(other.Id)); Assert.Equal(original.Id, moved.Id);
    }
    [Fact]
    public async Task ReadsTagsAndNeverRewritesTheFixture()
    {
        var path = Wave();
        using (var file = TagLib.File.Create(path))
        {
            file.Tag.Title = "Tagged"; file.Tag.Performers = ["Artist"]; file.Tag.AlbumArtists = ["Album artist"];
            file.Tag.Album = "Album"; file.Tag.Year = 2026; file.Tag.Genres = ["Test"]; file.Tag.Track = 2; file.Tag.Disc = 1; file.Save();
        }
        var bytes = await File.ReadAllBytesAsync(path); await Import(); var track = await OnlyTrack();
        Assert.Equal("Tagged", track.Title); Assert.Equal("Artist", track.Artist); Assert.Equal("Album artist", track.AlbumArtist);
        Assert.Equal("Album", track.Album); Assert.Equal((uint)2026, track.Year); Assert.Equal("Test", track.Genre);
        Assert.Equal((uint)2, track.TrackNumber); Assert.Equal((uint)1, track.DiscNumber); Assert.Equal(128, track.Bitrate);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
    }
    [Fact]
    public async Task SupportsAllRequestedExtensionsAndIgnoresOthers()
    {
        Wave();
        foreach (var extension in new[] { "MP3", "flac", "m4a", "aac", "ogg", "opus", "wma" })
            await File.WriteAllTextAsync(Path.Combine(Music, "fixture." + extension), "corrupt fixture");
        await File.WriteAllTextAsync(Path.Combine(Music, "ignore.txt"), "not audio");
        var p = await Analyze(); Assert.Equal(8, p.Result.New); Assert.Equal(7, p.Result.Errors);
        await scanner.ApplyAsync(p.Id); Assert.Equal(8, (await repository.GetTracksAsync(folder.Id)).Count);
    }
    [Fact]
    public async Task RootRecoveryRestoresAvailableState()
    {
        Wave(); await Import(); Directory.Move(Music, Music + "-offline"); await Import();
        Directory.Move(Music + "-offline", Music); var p = await Analyze(); Assert.Equal(1, p.Result.Unchanged);
        await scanner.ApplyAsync(p.Id); Assert.Equal(FileStatus.Available, (await OnlyTrack()).Status);
    }
    [Fact]
    public async Task RepeatedRequestedRootIsScannedOnce()
    {
        Wave(); var p = await scanner.AnalyzeAsync([folder, folder]); Assert.Equal(1, p.Result.New);
    }
    [Fact]
    public async Task MetadataFailurePreservesExistingValuesAndRetries()
    {
        var path = Wave(); await Import(); var original = await OnlyTrack();
        await repository.SaveTrackAsync(original with { Title = "Keep title", Artist = "Keep artist", Album = "Keep album" });
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1)); metadata.DenyPath = path;
        var p = await Analyze(); await scanner.ApplyAsync(p.Id); var after = await OnlyTrack();
        Assert.Equal("Keep title", after.Title); Assert.Equal("Keep artist", after.Artist); Assert.Equal("Keep album", after.Album);
        metadata.DenyPath = null; metadata.Reads = 0; p = await Analyze(); Assert.Equal(1, metadata.Reads); await scanner.ApplyAsync(p.Id);
    }
    [Fact]
    public async Task ReconnectedButUnreadableFileRemainsUnavailable()
    {
        var path = Wave(); await Import(); Directory.Move(Music, Music + "-offline"); await Import();
        Directory.Move(Music + "-offline", Music); fingerprints.DenyPath = path;
        var p = await Analyze(); Assert.Equal(1, p.Result.Unavailable); Assert.True(p.Result.Errors > 0);
        await scanner.ApplyAsync(p.Id); Assert.Equal(FileStatus.Unavailable, (await OnlyTrack()).Status);
    }
    [Fact]
    public async Task DiscoveryAccessExceptionBecomesUnavailableInsteadOfAborting()
    {
        Wave(); await Import(); var other = new IncrementalLibraryScanner(repository, new ThrowingFileSystem(), metadata, fingerprints);
        var p = await other.AnalyzeAsync([folder]); Assert.Equal(1, p.Result.Unavailable); Assert.True(p.Result.Errors > 0); Assert.Equal(0, p.Result.Missing);
    }
    private sealed class ThrowingFileSystem : IMusicFileSystem
    {
        public Task<FolderObservation> DiscoverAsync(MusicFolder folder, CancellationToken cancellationToken = default) => throw new UnauthorizedAccessException("Inaccessible root");
    }
    private sealed class CountingMetadata : IFileMetadataReader
    {
        public int Reads; public string? DenyPath;
        public async Task<TrackMetadata> ReadAsync(string path, CancellationToken cancellationToken = default) { Reads++; if (path == DenyPath) throw new IOException("Transient metadata failure"); return await new TagLibMetadataReader().ReadAsync(path, cancellationToken); }
    }
    private sealed class CountingFingerprinter : IFileFingerprinter
    {
        public int Hashes; public string? DenyPath;
        public Task<FileFingerprint> ReadAsync(string path, bool includeContentHash, CancellationToken cancellationToken = default)
        {
            if (path == DenyPath) throw new UnauthorizedAccessException("test inaccessible file");
            if (includeContentHash) Hashes++; return new StreamingFileFingerprinter().ReadAsync(path, includeContentHash, cancellationToken);
        }
    }
    private sealed class ImmediateProgress(Action<ScanProgress> report) : IProgress<ScanProgress> { public void Report(ScanProgress value) => report(value); }
    private sealed class PartialFileSystem : IMusicFileSystem
    {
        public Task<FolderObservation> DiscoverAsync(MusicFolder folder, CancellationToken cancellationToken = default) => Task.FromResult(new FolderObservation(folder.Id, false, [], [new ScanIssue(folder.Path, "Access denied")]));
    }
}
