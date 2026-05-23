namespace SpotyDuckTray;

public sealed class DuckingEngine
{
    private AppSettings _settings;
    private DuckingState _state = DuckingState.Idle;
    private DateTimeOffset? _externalAudioSince;
    private DateTimeOffset? _silenceSince;

    public DuckingEngine(AppSettings settings)
    {
        _settings = settings;
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
    }

    public void Update(bool isEnabled, bool externalAudioDetected, SpotifyController spotifyController, DateTimeOffset now)
    {
        spotifyController.UpdateFade(now);

        if (!isEnabled)
        {
            spotifyController.RestoreImmediately();
            Reset();
            return;
        }

        if (!spotifyController.IsAvailable)
        {
            Reset();
            return;
        }

        switch (_state)
        {
            case DuckingState.Idle:
                if (externalAudioDetected)
                {
                    _externalAudioSince ??= now;
                    if (ElapsedMs(_externalAudioSince.Value, now) >= _settings.AttackMs)
                    {
                        spotifyController.BeginDuck(GetDuckTarget(), _settings.FadeDurationMs, now);
                        _state = DuckingState.Ducked;
                        _silenceSince = null;
                    }
                    else
                    {
                        _state = DuckingState.PendingDuck;
                    }
                }
                break;

            case DuckingState.PendingDuck:
                if (!externalAudioDetected)
                {
                    Reset();
                    break;
                }

                _externalAudioSince ??= now;
                if (ElapsedMs(_externalAudioSince.Value, now) >= _settings.AttackMs)
                {
                    spotifyController.BeginDuck(GetDuckTarget(), _settings.FadeDurationMs, now);
                    _state = DuckingState.Ducked;
                    _silenceSince = null;
                }
                break;

            case DuckingState.Ducked:
                if (externalAudioDetected)
                {
                    _silenceSince = null;
                    spotifyController.BeginDuck(GetDuckTarget(), _settings.FadeDurationMs, now);
                    break;
                }

                _silenceSince ??= now;
                if (ElapsedMs(_silenceSince.Value, now) >= _settings.ReleaseMs)
                {
                    spotifyController.BeginRestore(_settings.FadeDurationMs, now);
                    Reset();
                }
                break;
        }
    }

    public void RestoreImmediately(SpotifyController spotifyController)
    {
        spotifyController.RestoreImmediately();
        Reset();
    }

    private float GetDuckTarget()
    {
        return _settings.GamingModeEnabled ? _settings.GamingDuckLevel : _settings.DuckLevel;
    }

    private void Reset()
    {
        _state = DuckingState.Idle;
        _externalAudioSince = null;
        _silenceSince = null;
    }

    private static double ElapsedMs(DateTimeOffset start, DateTimeOffset end)
    {
        return (end - start).TotalMilliseconds;
    }
}

public enum DuckingState
{
    Idle,
    PendingDuck,
    Ducked
}

// Made with Bob
