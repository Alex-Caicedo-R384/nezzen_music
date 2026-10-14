namespace Nezzen.Core;

public sealed record FileFingerprint
{
    public long Size { get; }
    public DateTimeOffset LastWriteUtc { get; }
    public string? ContentHash { get; }
    public FileFingerprint(long size, DateTimeOffset lastWriteUtc, string? contentHash = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        Size = size; LastWriteUtc = lastWriteUtc.ToUniversalTime(); ContentHash = contentHash;
    }
}
