using Nezzen.Core;
namespace Nezzen.Application;

public interface ILibraryRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SaveFolderAsync(MusicFolder folder, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MusicFolder>> GetFoldersAsync(CancellationToken cancellationToken = default);
    Task SaveTrackAsync(Track track, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Track>> GetTracksAsync(Guid folderId, CancellationToken cancellationToken = default);
    Task<LibrarySnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
    Task CommitScanAsync(long expectedRevision, IReadOnlyList<Track> tracks, ScanSession session,
        ScanResult result, IReadOnlyList<ScanEntry> entries, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScanSession>> GetScanHistoryAsync(CancellationToken cancellationToken = default);
    Task<ScanResult?> GetScanResultAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScanEntry>> GetScanEntriesAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
