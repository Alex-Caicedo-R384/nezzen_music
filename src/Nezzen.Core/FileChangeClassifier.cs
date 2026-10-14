namespace Nezzen.Core;

public static class FileChangeClassifier
{
    // Availability must be established before interpreting an absent entry.
    public static FileChange Classify(FileFingerprint? previous, FileFingerprint? current, bool folderAvailable)
    {
        if (!folderAvailable) return FileChange.Unavailable;
        if (current is null) return FileChange.Missing;
        if (previous is null) return FileChange.New;
        return previous.Size == current.Size && previous.LastWriteUtc == current.LastWriteUtc
            && (previous.ContentHash is null || current.ContentHash is null || previous.ContentHash == current.ContentHash)
            ? FileChange.Unchanged : FileChange.Modified;
    }
}
