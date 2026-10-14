using Nezzen.Core;
namespace Nezzen.Application;

public sealed record TrackMetadata(string Title, string? Artist, string? Album, TimeSpan Duration, string? Format)
{
    public string? AlbumArtist { get; init; }
    public uint? TrackNumber { get; init; }
    public uint? DiscNumber { get; init; }
    public uint? Year { get; init; }
    public string? Genre { get; init; }
    public int? Bitrate { get; init; }
    public int? SampleRate { get; init; }
}
public interface IFileMetadataReader { Task<TrackMetadata> ReadAsync(string path, CancellationToken cancellationToken = default); }
public interface IFileFingerprinter { Task<FileFingerprint> ReadAsync(string path, bool includeContentHash, CancellationToken cancellationToken = default); }
