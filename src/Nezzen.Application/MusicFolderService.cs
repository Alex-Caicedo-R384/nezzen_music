using Nezzen.Core;
namespace Nezzen.Application;

public sealed class MusicFolderService(ILibraryRepository repository)
{
    public Task<IReadOnlyList<MusicFolder>> GetAsync(CancellationToken cancellationToken = default) => repository.GetFoldersAsync(cancellationToken);
    public async Task<MusicFolder> RegisterAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        path = LibraryPath.Normalize(path);
        if (LibraryPath.HasLinkAncestor(path)) throw new ArgumentException("Las raíces no pueden atravesar enlaces simbólicos.", nameof(path));
        var folders = await repository.GetFoldersAsync(cancellationToken);
        var existing = folders.FirstOrDefault(f => LibraryPath.Comparer.Equals(f.Path, path));
        if (existing is not null) return existing;
        if (folders.Any(f => LibraryPath.Contains(f.Path, path) || LibraryPath.Contains(path, f.Path)))
            throw new ArgumentException("La carpeta se solapa con una raíz registrada.", nameof(path));
        var folder = new MusicFolder(Guid.NewGuid(), path);
        await repository.SaveFolderAsync(folder, cancellationToken);
        return folder;
    }
    public async Task SetEnabledAsync(Guid id, bool enabled, CancellationToken cancellationToken = default)
    {
        var folder = (await GetAsync(cancellationToken)).SingleOrDefault(f => f.Id == id)
            ?? throw new ArgumentException("Carpeta no registrada.", nameof(id));
        await repository.SaveFolderAsync(folder with { Enabled = enabled }, cancellationToken);
    }
}
