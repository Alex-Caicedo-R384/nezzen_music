namespace Nezzen.Core;

public static class LibraryPath
{
    public static StringComparer Comparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }
    public static string Key(string path) => OperatingSystem.IsWindows() ? Normalize(path).ToUpperInvariant() : Normalize(path);
    public static bool Contains(string parent, string child)
    {
        parent = Normalize(parent); child = Normalize(child);
        return Comparer.Equals(parent, child) || child.StartsWith(Path.EndsInDirectorySeparator(parent) ? parent : parent + Path.DirectorySeparatorChar,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
    public static bool HasLinkAncestor(string path)
    {
        for (var current = new DirectoryInfo(Normalize(path)); current is not null; current = current.Parent)
            if (current.LinkTarget is not null || (current.Exists && current.Attributes.HasFlag(FileAttributes.ReparsePoint))) return true;
        return false;
    }
}
