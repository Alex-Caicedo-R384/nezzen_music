using System.Security.Cryptography;
using Nezzen.Application;
using Nezzen.Core;

namespace Nezzen.Infrastructure;

public sealed class StreamingFileFingerprinter : IFileFingerprinter
{
    public async Task<FileFingerprint> ReadAsync(string path, bool includeContentHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var info = new FileInfo(path);
        if (info.Attributes.HasFlag(FileAttributes.ReparsePoint) || LibraryPath.HasLinkAncestor(Path.GetDirectoryName(path)!))
            throw new IOException("No se siguen enlaces al leer archivos.");
        var before = new FileFingerprint(info.Length, info.LastWriteTimeUtc);
        if (!includeContentHash) return before;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
        info.Refresh();
        if (info.Length != before.Size || info.LastWriteTimeUtc != before.LastWriteUtc.UtcDateTime)
            throw new IOException("El archivo cambió durante el cálculo del hash.");
        return new(before.Size, before.LastWriteUtc, hash);
    }
}
