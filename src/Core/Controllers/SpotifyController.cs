using NAudio.CoreAudioApi;

namespace SpotyDuckTray;

public sealed class SpotifyController
{
    private SimpleAudioVolume? _spotifyVolume;
    private float? _savedVolume;
    private float? _fadeStartVolume;
    private float? _fadeTargetVolume;
    private DateTimeOffset? _fadeStartTime;
    private int _fadeDurationMs;
    private bool _restoreAfterFade;
    private bool _isDucked;

    public bool IsAvailable => _spotifyVolume is not null;
    public bool IsDucked => _isDucked;
    public bool IsFading => _spotifyVolume is not null && _fadeTargetVolume.HasValue && _fadeStartTime.HasValue;

    public void UpdateSession(SimpleAudioVolume? spotifyVolume)
    {
        _spotifyVolume = spotifyVolume;
    }

    public void BeginDuck(float duckLevel, int fadeDurationMs, DateTimeOffset now)
    {
        if (_spotifyVolume is null)
        {
            return;
        }

        if (!_savedVolume.HasValue)
        {
            _savedVolume = _spotifyVolume.Volume;
        }

        _restoreAfterFade = false;
        _isDucked = true;
        StartFade(duckLevel, fadeDurationMs, now);
    }

    public void BeginRestore(int fadeDurationMs, DateTimeOffset now)
    {
        if (_spotifyVolume is null)
        {
            ClearState();
            return;
        }

        if (!_savedVolume.HasValue)
        {
            _isDucked = false;
            StopFade();
            return;
        }

        _restoreAfterFade = true;
        StartFade(_savedVolume.Value, fadeDurationMs, now);
    }

    public void UpdateFade(DateTimeOffset now)
    {
        if (_spotifyVolume is null || !_fadeTargetVolume.HasValue || !_fadeStartTime.HasValue || !_fadeStartVolume.HasValue)
        {
            return;
        }

        if (_fadeDurationMs <= 0)
        {
            CompleteFade();
            return;
        }

        var elapsedMs = (now - _fadeStartTime.Value).TotalMilliseconds;
        var progress = Math.Clamp((float)(elapsedMs / _fadeDurationMs), 0f, 1f);
        var currentVolume = _fadeStartVolume.Value + ((_fadeTargetVolume.Value - _fadeStartVolume.Value) * progress);
        _spotifyVolume.Volume = currentVolume;

        if (progress >= 1f)
        {
            CompleteFade();
        }
    }

    public void RestoreImmediately()
    {
        if (_spotifyVolume is null)
        {
            ClearState();
            return;
        }

        if (_savedVolume.HasValue)
        {
            _spotifyVolume.Volume = _savedVolume.Value;
        }

        ClearState();
    }

    private void StartFade(float targetVolume, int fadeDurationMs, DateTimeOffset now)
    {
        if (_spotifyVolume is null)
        {
            return;
        }

        _fadeStartVolume = _spotifyVolume.Volume;
        _fadeTargetVolume = targetVolume;
        _fadeStartTime = now;
        _fadeDurationMs = Math.Max(0, fadeDurationMs);

        if (_fadeDurationMs == 0)
        {
            CompleteFade();
        }
    }

    private void CompleteFade()
    {
        if (_spotifyVolume is not null && _fadeTargetVolume.HasValue)
        {
            _spotifyVolume.Volume = _fadeTargetVolume.Value;
        }

        if (_restoreAfterFade)
        {
            ClearState();
            return;
        }

        StopFade();
        _isDucked = true;
    }

    private void StopFade()
    {
        _fadeStartVolume = null;
        _fadeTargetVolume = null;
        _fadeStartTime = null;
        _fadeDurationMs = 0;
        _restoreAfterFade = false;
    }

    private void ClearState()
    {
        StopFade();
        _savedVolume = null;
        _isDucked = false;
    }
}


