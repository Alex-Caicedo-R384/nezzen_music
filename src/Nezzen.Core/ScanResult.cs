namespace Nezzen.Core;

public enum FileChange { New, Modified, Unchanged, Missing, Unavailable, PossibleMove, PossibleDuplicate, Moved, Error }
public enum MatchConfidence { None, MetadataCandidate, ContentHash }
public sealed record ScanIssue(string Path, string Message);
public sealed record ScanResult(Guid SessionId, int New, int Modified, int Unchanged, int Missing, int Unavailable, int PossibleMoves, int PossibleDuplicates, IReadOnlyList<ScanIssue> Issues)
{
    public int Moved { get; init; }
    public int Errors => Issues.Count;
}
public sealed record ScanProgress(long Processed, string? CurrentPath)
{
    public long Found { get; init; }
    public int Errors { get; init; }
    public string State { get; init; } = "Analizando";
}
public sealed record ScanEntry(string Path, FileChange Change, Guid? TrackId = null, string? PreviousPath = null,
    MatchConfidence Confidence = MatchConfidence.None, string? Message = null);
