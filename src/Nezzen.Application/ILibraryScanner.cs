using Nezzen.Core;
namespace Nezzen.Application;

public interface ILibraryScanner
{
    Task<ScanResult> ScanAsync(IReadOnlyList<MusicFolder> folders, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<ScanPreview> AnalyzeAsync(IReadOnlyList<MusicFolder> folders, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<ScanResult> ApplyAsync(Guid previewId, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default);
}
