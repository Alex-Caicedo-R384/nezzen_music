using Nezzen.Application;

namespace Nezzen.Infrastructure;

public sealed class TagLibMetadataReader : IFileMetadataReader
{
    public Task<TrackMetadata> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            // Abstraction physically forbids write access; never call Save().
            using var file = Path.GetExtension(path).Equals(".opus", StringComparison.OrdinalIgnoreCase)
                ? TagLib.File.Create(new ReadOnlyFile(path), "taglib/ogg", TagLib.ReadStyle.Average)
                : TagLib.File.Create(new ReadOnlyFile(path));
            var tag = file.Tag; var audio = file.Properties;
            var result = new TrackMetadata(string.IsNullOrWhiteSpace(tag.Title) ? (string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(path)) ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path)) : tag.Title,
                Join(tag.Performers), tag.Album, audio.Duration, Path.GetExtension(path).TrimStart('.').ToLowerInvariant())
            {
                AlbumArtist = Join(tag.AlbumArtists),
                TrackNumber = Positive(tag.Track),
                DiscNumber = Positive(tag.Disc),
                Year = Positive(tag.Year),
                Genre = Join(tag.Genres),
                Bitrate = audio.AudioBitrate,
                SampleRate = audio.AudioSampleRate
            };
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
        catch (Exception e) when (e is TagLib.CorruptFileException or TagLib.UnsupportedFormatException or ArgumentException or IndexOutOfRangeException or OverflowException)
        { throw new InvalidDataException("Audio inválido o formato no soportado por TagLib#.", e); }
    }
    private static uint? Positive(uint value) => value == 0 ? null : value;
    private static string? Join(string[] values) => values.Length == 0 ? null : string.Join("; ", values);
    private sealed class ReadOnlyFile(string path) : TagLib.File.IFileAbstraction
    {
        public string Name => path;
        public Stream ReadStream => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        public Stream WriteStream => throw new NotSupportedException("La lectura de metadatos no admite escritura.");
        public void CloseStream(Stream stream) => stream.Dispose();
    }
}
