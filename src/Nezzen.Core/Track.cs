namespace Nezzen.Core;

public enum FileStatus { Available, Missing, Unavailable }
public sealed record Track
{
    public Guid Id { get; }
    public Guid FolderId { get; init; }
    private string path = "";
    public string Path { get => path; init { ArgumentException.ThrowIfNullOrWhiteSpace(value); if (!System.IO.Path.IsPathFullyQualified(value)) throw new ArgumentException("Absolute path required.", nameof(value)); path = System.IO.Path.GetFullPath(value); } }
    private string title = "";
    public string Title
    {
        get => title;
        init { ArgumentException.ThrowIfNullOrWhiteSpace(value); title = value; }
    }
    private FileFingerprint fingerprint = null!;
    public FileFingerprint Fingerprint
    {
        get => fingerprint;
        init { ArgumentNullException.ThrowIfNull(value); fingerprint = value; }
    }
    public FileStatus Status { get; init; } = FileStatus.Available;
    public TimeSpan Duration { get; init; }
    public string? Format { get; init; }
    public bool MetadataReadFailed { get; init; }
    public string? Artist { get; init; }
    public string? AlbumArtist { get; init; }
    public string? Album { get; init; }
    public uint? Year { get; init; }
    public string? Genre { get; init; }
    public int? Bitrate { get; init; }
    public int? SampleRate { get; init; }
    public Guid? ArtistId { get; init; }
    public Guid? AlbumId { get; init; }
    public uint? TrackNumber { get; init; }
    public uint? DiscNumber { get; init; }
    public DateTimeOffset AddedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastVerifiedUtc { get; init; }
    private double? playbackSpeed;
    public double? PlaybackSpeed { get => playbackSpeed; init { if (value is double speed && (!double.IsFinite(speed) || speed <= 0)) throw new ArgumentOutOfRangeException(nameof(value)); playbackSpeed = value; } }
    public Track(Guid id, Guid folderId, string path, string title, FileFingerprint fingerprint)
    {
        if (id == Guid.Empty || folderId == Guid.Empty) throw new ArgumentException("Non-empty identifiers required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(title); ArgumentNullException.ThrowIfNull(fingerprint);
        Id = id; FolderId = folderId; Path = path; Title = title; Fingerprint = fingerprint;
    }
}
