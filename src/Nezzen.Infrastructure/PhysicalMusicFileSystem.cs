using Nezzen.Application;
using Nezzen.Core;

namespace Nezzen.Infrastructure;

public sealed class PhysicalMusicFileSystem : IMusicFileSystem
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".opus", ".wma" };

    public Task<FolderObservation> DiscoverAsync(MusicFolder folder, CancellationToken cancellationToken = default)
    {
        var files = new List<FileObservation>();
        var issues = new List<ScanIssue>();
        var directories = new Stack<string>(); directories.Push(folder.Path);
        if (LibraryPath.HasLinkAncestor(folder.Path))
            return Task.FromResult(new FolderObservation(folder.Id, false, [], [new(folder.Path, "Raíz con enlace simbólico: no se sigue.")]));
        while (directories.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (LibraryPath.HasLinkAncestor(directory))
                {
                    issues.Add(new(directory, "Directorio con enlace omitido.")); continue;
                }
                foreach (var path in Directory.EnumerateFileSystemEntries(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var attributes = File.GetAttributes(path);
                        if (attributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            issues.Add(new(path, "Enlace simbólico omitido.")); continue;
                        }
                        if (attributes.HasFlag(FileAttributes.Directory)) directories.Push(path);
                        else if (Extensions.Contains(Path.GetExtension(path)))
                        {
                            var info = new FileInfo(path);
                            files.Add(new(LibraryPath.Normalize(path), new(info.Length, info.LastWriteTimeUtc)));
                        }
                    }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException) { issues.Add(new(path, e.Message)); }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { issues.Add(new(directory, e.Message)); }
        }
        return Task.FromResult(new FolderObservation(folder.Id, issues.Count == 0, files.AsReadOnly(), issues.AsReadOnly()));
    }
}
