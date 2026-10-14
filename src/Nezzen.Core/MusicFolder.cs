namespace Nezzen.Core;

public sealed record MusicFolder
{
    public Guid Id { get; }
    public bool Enabled { get; init; } = true;
    public string Path { get; }
    public MusicFolder(Guid id, string path)
    {
        if (id == Guid.Empty) throw new ArgumentException("Non-empty identifier required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!System.IO.Path.IsPathFullyQualified(path)) throw new ArgumentException("Absolute path required.", nameof(path));
        Id = id; Path = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(path));
    }
}
