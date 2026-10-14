using Nezzen.Core;
using Xunit;
namespace Nezzen.Core.Tests;

public class DomainTests
{
    private static FileFingerprint Fingerprint(long size = 10) => new(size, DateTimeOffset.UnixEpoch);
    [Fact] public void TrackRejectsEmptyIdentity() => Assert.Throws<ArgumentException>(() => new Track(Guid.Empty, Guid.NewGuid(), "/song.wav", "Song", Fingerprint()));
    [Fact] public void TrackRejectsRelativePath() => Assert.Throws<ArgumentException>(() => new Track(Guid.NewGuid(), Guid.NewGuid(), "song.wav", "Song", Fingerprint()));
    [Fact] public void FingerprintRejectsNegativeSize() => Assert.Throws<ArgumentOutOfRangeException>(() => Fingerprint(-1));
    [Fact]
    public void UpdatingLocationPreservesIdentityAndPreferences()
    {
        var track = new Track(Guid.NewGuid(), Guid.NewGuid(), Path.GetFullPath("a.wav"), "Song", Fingerprint()) { PlaybackSpeed = 1.25 };
        var moved = track with { Path = Path.GetFullPath("b.wav") };
        Assert.Equal(track.Id, moved.Id); Assert.Equal(1.25, moved.PlaybackSpeed);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidSpeedIsRejected(double speed) => Assert.Throws<ArgumentOutOfRangeException>(() => new Track(Guid.NewGuid(), Guid.NewGuid(), Path.GetFullPath("a.wav"), "Song", Fingerprint()) { PlaybackSpeed = speed });
    [Fact] public void SameFileInfoIsUnchanged() => Assert.Equal(FileChange.Unchanged, FileChangeClassifier.Classify(Fingerprint(), Fingerprint(), true));
    [Fact] public void ChangedSizeRequiresProcessing() => Assert.Equal(FileChange.Modified, FileChangeClassifier.Classify(Fingerprint(), Fingerprint(20), true));
    [Fact] public void UnknownFileIsNew() => Assert.Equal(FileChange.New, FileChangeClassifier.Classify(null, Fingerprint(), true));
    [Fact] public void DisconnectedFolderDoesNotMeanDeleted() => Assert.Equal(FileChange.Unavailable, FileChangeClassifier.Classify(Fingerprint(), null, false));
    [Fact] public void AbsentFileInAvailableFolderIsMissing() => Assert.Equal(FileChange.Missing, FileChangeClassifier.Classify(Fingerprint(), null, true));
    [Fact]
    public void UpdatesCannotBypassTitleValidation()
    {
        var track = new Track(Guid.NewGuid(), Guid.NewGuid(), Path.GetFullPath("a.wav"), "Song", Fingerprint());
        Assert.Throws<ArgumentException>(() => track with { Title = "" });
    }
    [Fact]
    public void UpdatesCannotRemoveFingerprint()
    {
        var track = new Track(Guid.NewGuid(), Guid.NewGuid(), Path.GetFullPath("a.wav"), "Song", Fingerprint());
        Assert.Throws<ArgumentNullException>(() => track with { Fingerprint = null! });
    }
}
