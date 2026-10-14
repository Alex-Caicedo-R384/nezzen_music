using System.Collections;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Nezzen.Application;
using Nezzen.Core;
using Nezzen.Infrastructure;
using Xunit;

namespace Nezzen.Infrastructure.Tests;

public sealed class ScanTransactionTests : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "nezzen-tx-" + Guid.NewGuid());
    private string Database => Path.Combine(root, "library.db");
    public Task InitializeAsync() { Directory.CreateDirectory(root); return Task.CompletedTask; }
    public Task DisposeAsync() { Directory.Delete(root, true); return Task.CompletedTask; }
    private async Task Sql(string sql)
    {
        using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database, Pooling = false }.ToString()); await c.OpenAsync();
        using var cmd = c.CreateCommand(); cmd.CommandText = sql; await cmd.ExecuteNonQueryAsync();
    }
    private Track Track(Guid folder, string name) => new(Guid.NewGuid(), folder, Path.Combine(root, name), name, new FileFingerprint(1, DateTimeOffset.UnixEpoch));
    private static ScanResult Result(Guid session) => new(session, 2, 0, 0, 0, 0, 0, 0, []);
    [Fact]
    public async Task DatabaseFailureRollsBackTracksHistoryAndRevision()
    {
        var repo = new SqliteLibraryRepository(Database); await repo.InitializeAsync(); var folder = new MusicFolder(Guid.NewGuid(), root); await repo.SaveFolderAsync(folder);
        var before = await repo.GetSnapshotAsync();
        await Sql("CREATE TRIGGER reject_session BEFORE INSERT ON scan_sessions BEGIN SELECT RAISE(ABORT, 'test rollback'); END;");
        var id = Guid.NewGuid(); var tracks = new[] { Track(folder.Id, "a.wav"), Track(folder.Id, "b.wav") };
        var session = new ScanSession(id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, ScanState.Completed);
        await Assert.ThrowsAsync<SqliteException>(() => repo.CommitScanAsync(before.Revision, tracks, session, Result(id), []));
        Assert.Empty(await repo.GetTracksAsync(folder.Id)); Assert.Empty(await repo.GetScanHistoryAsync()); Assert.Equal(before.Revision, (await repo.GetSnapshotAsync()).Revision);
        await Sql("DROP TRIGGER reject_session;");
        await repo.CommitScanAsync(before.Revision, tracks, session, Result(id), []); Assert.Equal(2, (await repo.GetTracksAsync(folder.Id)).Count);
    }
    [Fact]
    public async Task CancellationDuringBatchRollsBackEarlierWrites()
    {
        var repo = new SqliteLibraryRepository(Database); await repo.InitializeAsync(); var folder = new MusicFolder(Guid.NewGuid(), root); await repo.SaveFolderAsync(folder);
        var before = await repo.GetSnapshotAsync(); using var cts = new CancellationTokenSource(); var id = Guid.NewGuid();
        var tracks = new CancelDuringEnumeration([Track(folder.Id, "a.wav"), Track(folder.Id, "b.wav")], cts);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.CommitScanAsync(before.Revision, tracks,
            new(id, DateTimeOffset.UtcNow, null, ScanState.Completed), Result(id), [], cts.Token));
        Assert.Empty(await repo.GetTracksAsync(folder.Id)); Assert.Empty(await repo.GetScanHistoryAsync()); Assert.Equal(before.Revision, (await repo.GetSnapshotAsync()).Revision);
    }
    [Fact]
    public async Task StaleRevisionRejectedByTransaction()
    {
        var repo = new SqliteLibraryRepository(Database); await repo.InitializeAsync(); var folder = new MusicFolder(Guid.NewGuid(), root); await repo.SaveFolderAsync(folder);
        var before = await repo.GetSnapshotAsync(); await repo.SaveFolderAsync(folder with { Enabled = false }); var id = Guid.NewGuid();
        await Assert.ThrowsAsync<StaleScanPreviewException>(() => repo.CommitScanAsync(before.Revision, [Track(folder.Id, "a.wav")],
            new(id, DateTimeOffset.UtcNow, null, ScanState.Completed), Result(id), [])); Assert.Empty(await repo.GetTracksAsync(folder.Id));
    }
    [Fact]
    public async Task MigratesVersionOneWithoutLosingTrackIdentityOrSpeed()
    {
        await Sql("CREATE TABLE music_folders(id TEXT PRIMARY KEY,path TEXT NOT NULL UNIQUE); CREATE TABLE tracks(id TEXT PRIMARY KEY,folder_id TEXT NOT NULL REFERENCES music_folders(id),path TEXT NOT NULL UNIQUE,payload TEXT NOT NULL); PRAGMA user_version=1;");
        var folder = new MusicFolder(Guid.NewGuid(), root); var original = Track(folder.Id, "legacy.wav") with { PlaybackSpeed = 0.9 };
        using (var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database, Pooling = false }.ToString()))
        {
            await c.OpenAsync(); using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO music_folders VALUES($folder,$root); INSERT INTO tracks VALUES($id,$folder,$path,$payload);";
            cmd.Parameters.AddWithValue("$folder", folder.Id.ToString()); cmd.Parameters.AddWithValue("$root", root); cmd.Parameters.AddWithValue("$id", original.Id.ToString());
            cmd.Parameters.AddWithValue("$path", original.Path); cmd.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(original)); await cmd.ExecuteNonQueryAsync();
        }
        var repo = new SqliteLibraryRepository(Database); await repo.InitializeAsync(); await repo.InitializeAsync();
        var track = Assert.Single(await repo.GetTracksAsync(folder.Id)); Assert.Equal(original.Id, track.Id); Assert.Equal(0.9, track.PlaybackSpeed);
        Assert.True(Assert.Single(await repo.GetFoldersAsync()).Enabled);
    }
    private sealed class CancelDuringEnumeration(IReadOnlyList<Track> tracks, CancellationTokenSource cts) : IReadOnlyList<Track>
    {
        public int Count => tracks.Count;
        public Track this[int index] => tracks[index];
        public IEnumerator<Track> GetEnumerator() { yield return tracks[0]; cts.Cancel(); yield return tracks[1]; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
