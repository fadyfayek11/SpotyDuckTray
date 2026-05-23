using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace SpotyDuckTray;

public sealed class AudioMonitor : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator;
    private MMDevice? _device;
    private bool _disposed;

    public AudioMonitor()
    {
        _enumerator = new MMDeviceEnumerator();
        AppLogger.Info("Audio monitor initialized");
    }

    public AudioSnapshot Capture(float threshold)
    {
        var snapshot = new AudioSnapshot();
        var device = GetDevice();

        if (device is null)
        {
            return snapshot;
        }

        SessionCollection? sessions = null;

        try
        {
            sessions = device.AudioSessionManager.Sessions;
        }
        catch (Exception exception)
        {
            AppLogger.Error("Reading audio sessions failed", exception);
            return snapshot;
        }

        for (var i = 0; i < sessions.Count; i++)
        {
            AudioSessionControl? session = null;

            try
            {
                session = sessions[i];
                var pid = (int)session.GetProcessID;

                if (pid == 0)
                {
                    continue;
                }

                using var process = Process.GetProcessById(pid);
                var processName = NormalizeProcessName(process.ProcessName);

                if (string.IsNullOrWhiteSpace(processName))
                {
                    continue;
                }

                var peak = session.AudioMeterInformation.MasterPeakValue;
                var sessionInfo = new AudioSessionInfo
                {
                    ProcessId = pid,
                    ProcessName = processName,
                    Peak = peak,
                    IsActive = peak > threshold
                };

                if (processName.Contains("spotify", StringComparison.OrdinalIgnoreCase))
                {
                    snapshot.SpotifyVolume = session.SimpleAudioVolume;
                    snapshot.SpotifyPeak = peak;
                    snapshot.SpotifySession = sessionInfo;
                    continue;
                }

                snapshot.ExternalSessions.Add(sessionInfo);
            }
            catch (Exception exception)
            {
                AppLogger.Warning($"Skipping audio session index {i} due to read failure: {exception.Message}");
            }
            finally
            {
                session?.Dispose();
            }
        }

        return snapshot;
    }

    private MMDevice? GetDevice()
    {
        try
        {
            _device?.Dispose();
            _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return _device;
        }
        catch (Exception exception)
        {
            AppLogger.Error("Getting default audio endpoint failed", exception);
            _device = null;
            return null;
        }
    }

    private static string NormalizeProcessName(string? processName)
    {
        return (processName ?? string.Empty).Trim().ToLowerInvariant();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _device?.Dispose();
        }
        catch (Exception exception)
        {
            AppLogger.Error("Disposing audio device failed", exception);
        }

        try
        {
            _enumerator.Dispose();
        }
        catch (Exception exception)
        {
            AppLogger.Error("Disposing audio enumerator failed", exception);
        }

        _disposed = true;
        AppLogger.Info("Audio monitor disposed");
    }
}

public sealed class AudioSnapshot
{
    public SimpleAudioVolume? SpotifyVolume { get; set; }
    public float SpotifyPeak { get; set; }
    public AudioSessionInfo? SpotifySession { get; set; }
    public List<AudioSessionInfo> ExternalSessions { get; } = [];
}

public sealed class AudioSessionInfo
{
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public float Peak { get; init; }
    public bool IsActive { get; init; }
}

// Made with Bob
