namespace Nezzen.Audio;
// Contract only: capability negotiation precedes choosing the audio engine.
public sealed record AudioCapabilities(bool Gapless, bool PitchPreservingSpeed, bool ReplayGain);
public interface IAudioPlayer : IAsyncDisposable
{
    AudioCapabilities Capabilities { get; }
    Task LoadAsync(string path, CancellationToken cancellationToken = default);
    Task PlayAsync(CancellationToken cancellationToken = default);
    Task PauseAsync(CancellationToken cancellationToken = default);
    Task SetSpeedAsync(double speed, bool preservePitch, CancellationToken cancellationToken = default);
}
