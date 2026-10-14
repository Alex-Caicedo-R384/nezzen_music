using Nezzen.Core;
namespace Nezzen.Application;

public sealed record ScanPreview(Guid Id, ScanResult Result, IReadOnlyList<ScanEntry> Entries);
public sealed class StaleScanPreviewException(string message) : InvalidOperationException(message);
public sealed record LibrarySnapshot(long Revision, IReadOnlyList<MusicFolder> Folders, IReadOnlyList<Track> Tracks);
public sealed record FileObservation(string Path, FileFingerprint Fingerprint);
public sealed record FolderObservation(Guid FolderId, bool Complete, IReadOnlyList<FileObservation> Files, IReadOnlyList<ScanIssue> Issues);
public interface IMusicFileSystem
{
    Task<FolderObservation> DiscoverAsync(MusicFolder folder, CancellationToken cancellationToken = default);
}
