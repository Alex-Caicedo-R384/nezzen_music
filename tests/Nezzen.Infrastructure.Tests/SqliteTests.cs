using Nezzen.Core;
using Nezzen.Infrastructure;
using Xunit;
namespace Nezzen.Infrastructure.Tests;

public class SqliteTests
{
    [Fact]
    public async Task PersistsUpdatesAndUnavailableTracksAcrossReopen()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nezzen-" + Guid.NewGuid()); Directory.CreateDirectory(dir);
        try
        {
            var db = Path.Combine(dir, "library.db"); var store = new SqliteLibraryRepository(db);
            await store.InitializeAsync(); await store.InitializeAsync();
            var folder = new MusicFolder(Guid.NewGuid(), dir); await store.SaveFolderAsync(folder);
            var track = new Track(Guid.NewGuid(), folder.Id, Path.Combine(dir, "fiction.wav"), "L'été", new FileFingerprint(12, DateTimeOffset.UnixEpoch)) { PlaybackSpeed = 1.25 };
            await store.SaveTrackAsync(track); await store.SaveTrackAsync(track with { Status = FileStatus.Unavailable, Title = "Updated" });
            var reopened = new SqliteLibraryRepository(db); await reopened.InitializeAsync();
            var tracks = await reopened.GetTracksAsync(folder.Id); var actual = Assert.Single(tracks);
            Assert.Equal(track.Id, actual.Id); Assert.Equal(FileStatus.Unavailable, actual.Status); Assert.Equal(1.25, actual.PlaybackSpeed); Assert.Equal("Updated", actual.Title);
            Assert.Equal(folder, Assert.Single(await reopened.GetFoldersAsync()));
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(dir, true); }
    }
    [Fact]
    public async Task InitializationHonorsCancellation()
    {
        var store = new SqliteLibraryRepository(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db"));
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.InitializeAsync(cts.Token));
    }
}
