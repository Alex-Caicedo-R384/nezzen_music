namespace Nezzen.Core;

public enum ScanState { Running, Completed, Cancelled, Failed }
public sealed record ScanSession(Guid Id, DateTimeOffset StartedUtc, DateTimeOffset? FinishedUtc, ScanState State);
