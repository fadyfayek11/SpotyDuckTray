using System.Drawing;
using System.Windows.Forms;

namespace SpotyDuckTray;

public sealed class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _duckingToggleMenuItem;
    private readonly ToolStripMenuItem _gamingModeMenuItem;
    private readonly ToolStripMenuItem _startupMenuItem;
    private readonly ToolStripMenuItem _hotkeyStatusMenuItem;
    private readonly ToolStripMenuItem _settingsMenuItem;
    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly ToolStripMenuItem _profileMenuItem;
    private readonly ToolStripMenuItem _defaultPresetMenuItem;
    private readonly ToolStripMenuItem _gamingPresetMenuItem;
    private readonly ToolStripMenuItem _voiceChatPresetMenuItem;
    private readonly ToolStripMenuItem _streamingPresetMenuItem;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly AudioMonitor _audioMonitor;
    private readonly SpotifyController _spotifyController;
    private readonly DuckingEngine _duckingEngine;
    private readonly StartupManager _startupManager;
    private readonly HotkeyManager _hotkeyManager;
    private readonly string _settingsPath;

    private AppSettings _settings;
    private List<string> _knownApps = [];
    private bool _isUpdatingAudio;
    private bool _disposed;

    public TrayAppContext()
    {
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        _settings = AppSettings.Load(_settingsPath);

        _audioMonitor = new AudioMonitor();
        _spotifyController = new SpotifyController();
        _duckingEngine = new DuckingEngine(_settings);
        _startupManager = new StartupManager();
        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

        _statusMenuItem = new ToolStripMenuItem("Status: Starting...")
        {
            Enabled = false
        };

        _duckingToggleMenuItem = new ToolStripMenuItem();
        _duckingToggleMenuItem.Click += ToggleDucking;

        _gamingModeMenuItem = new ToolStripMenuItem();
        _gamingModeMenuItem.Click += ToggleGamingMode;

        _startupMenuItem = new ToolStripMenuItem();
        _startupMenuItem.Click += ToggleStartup;

        _hotkeyStatusMenuItem = new ToolStripMenuItem("Hotkey: Not configured")
        {
            Enabled = false
        };

        _settingsMenuItem = new ToolStripMenuItem("Open Settings...", null, OpenSettings);

        _defaultPresetMenuItem = new ToolStripMenuItem("Default", null, (_, _) => ApplyPreset(BuiltInPreset.Default));
        _gamingPresetMenuItem = new ToolStripMenuItem("Gaming", null, (_, _) => ApplyPreset(BuiltInPreset.Gaming));
        _voiceChatPresetMenuItem = new ToolStripMenuItem("Voice Chat", null, (_, _) => ApplyPreset(BuiltInPreset.VoiceChat));
        _streamingPresetMenuItem = new ToolStripMenuItem("Streaming", null, (_, _) => ApplyPreset(BuiltInPreset.Streaming));

        _profileMenuItem = new ToolStripMenuItem("Profiles");
        _profileMenuItem.DropDownItems.Add(_defaultPresetMenuItem);
        _profileMenuItem.DropDownItems.Add(_gamingPresetMenuItem);
        _profileMenuItem.DropDownItems.Add(_voiceChatPresetMenuItem);
        _profileMenuItem.DropDownItems.Add(_streamingPresetMenuItem);

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_profileMenuItem);
        menu.Items.Add(_duckingToggleMenuItem);
        menu.Items.Add(_gamingModeMenuItem);
        menu.Items.Add(_startupMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_settingsMenuItem);
        menu.Items.Add(_hotkeyStatusMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, Exit);

        _trayIcon = new NotifyIcon
        {
            Icon = new Icon("Resources/logo.ico"),
            Visible = true,
            Text = "Spoty Duck",
            ContextMenuStrip = menu
        };

        _timer = new System.Windows.Forms.Timer();
        _timer.Tick += OnTimerTick;

        AppLogger.Info("Tray context initialized");

        var startupSyncedSettings = _settings.Clone();
        startupSyncedSettings.StartWithWindows = _startupManager.IsEnabled();
        ApplySettings(startupSyncedSettings, saveToDisk: startupSyncedSettings.StartWithWindows != _settings.StartWithWindows);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_disposed || _isUpdatingAudio)
        {
            return;
        }

        try
        {
            _isUpdatingAudio = true;

            var snapshot = _audioMonitor.Capture(_settings.Threshold);
            _spotifyController.UpdateSession(snapshot.SpotifyVolume);

            _knownApps = AudioRuleEngine.MergeDetectedApps(snapshot.ExternalSessions, _settings.AppRules);
            var shouldDuck = AudioRuleEngine.ShouldDuck(_settings, snapshot.ExternalSessions);

            _duckingEngine.Update(_settings.DuckingEnabled, shouldDuck, _spotifyController, DateTimeOffset.UtcNow);
            _statusMenuItem.Text = BuildStatusText(_settings, shouldDuck, _spotifyController.IsAvailable);
        }
        catch (Exception exception)
        {
            AppLogger.Error("Audio update tick failed", exception);
            _statusMenuItem.Text = "Status: Error - see log";
        }
        finally
        {
            _isUpdatingAudio = false;
        }
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        try
        {
            var updated = _settings.Clone();
            updated.DuckingEnabled = !updated.DuckingEnabled;
            ApplySettings(updated, saveToDisk: true);
            _trayIcon.ShowBalloonTip(1500, "Spoty Duck", updated.DuckingEnabled ? "Ducking enabled" : "Ducking paused", ToolTipIcon.Info);
            AppLogger.Info($"Hotkey toggled ducking to {(updated.DuckingEnabled ? "enabled" : "paused")}");
        }
        catch (Exception exception)
        {
            AppLogger.Error("Hotkey toggle failed", exception);
            _trayIcon.ShowBalloonTip(2000, "Spoty Duck", "Hotkey action failed. See log for details.", ToolTipIcon.Warning);
        }
    }

    private void ToggleDucking(object? sender, EventArgs e)
    {
        var updated = _settings.Clone();
        updated.DuckingEnabled = !updated.DuckingEnabled;
        updated.SelectedPreset = BuiltInPreset.Custom;
        ApplySettings(updated, saveToDisk: true);
    }

    private void ToggleGamingMode(object? sender, EventArgs e)
    {
        var updated = _settings.Clone();
        updated.GamingModeEnabled = !updated.GamingModeEnabled;
        updated.SelectedPreset = BuiltInPreset.Custom;
        ApplySettings(updated, saveToDisk: true);
    }

    private void ToggleStartup(object? sender, EventArgs e)
    {
        var updated = _settings.Clone();
        updated.StartWithWindows = !updated.StartWithWindows;
        ApplySettings(updated, saveToDisk: true);
    }

    private void OpenSettings(object? sender, EventArgs e)
    {
        try
        {
            using var form = new SettingsForm(_settings.Clone(), _knownApps);
            if (form.ShowDialog() != DialogResult.OK || form.SavedSettings is null)
            {
                return;
            }

            ApplySettings(form.SavedSettings, saveToDisk: true);
            _trayIcon.ShowBalloonTip(1500, "Spoty Duck", "Settings saved", ToolTipIcon.Info);
        }
        catch (Exception exception)
        {
            AppLogger.Error("Opening or saving settings failed", exception);
            _trayIcon.ShowBalloonTip(2000, "Spoty Duck", "Settings could not be opened or saved. See log for details.", ToolTipIcon.Warning);
        }
    }

    private void ApplyPreset(BuiltInPreset preset)
    {
        try
        {
            var presetSettings = _settings.ApplyPreset(preset);
            ApplySettings(presetSettings, saveToDisk: true);
            _trayIcon.ShowBalloonTip(1500, "Spoty Duck", $"{GetPresetDisplayName(preset)} preset applied", ToolTipIcon.Info);
            AppLogger.Info($"Applied built-in preset: {preset}");
        }
        catch (Exception exception)
        {
            AppLogger.Error($"Applying preset {preset} failed", exception);
            _trayIcon.ShowBalloonTip(2000, "Spoty Duck", "Preset could not be applied. See log for details.", ToolTipIcon.Warning);
        }
    }

    private void ApplySettings(AppSettings settings, bool saveToDisk)
    {
        var sanitized = AppSettings.Sanitize(settings);

        try
        {
            if (!_startupManager.Sync(sanitized.StartWithWindows))
            {
                AppLogger.Warning("Startup sync did not match desired state; using detected state");
                sanitized.StartWithWindows = _startupManager.IsEnabled();
            }
        }
        catch (Exception exception)
        {
            AppLogger.Error("Startup sync failed", exception);
            sanitized.StartWithWindows = false;
        }

        var hotkeyRegistered = false;
        try
        {
            if (sanitized.HotkeyEnabled)
            {
                hotkeyRegistered = _hotkeyManager.Register(sanitized.HotkeyModifiers, sanitized.HotkeyKey);
                if (!hotkeyRegistered)
                {
                    AppLogger.Warning($"Hotkey registration failed for {FormatHotkey(sanitized.HotkeyModifiers, sanitized.HotkeyKey)}");
                    sanitized.HotkeyEnabled = false;
                    _hotkeyManager.Unregister();
                }
            }
            else
            {
                _hotkeyManager.Unregister();
            }
        }
        catch (Exception exception)
        {
            AppLogger.Error("Hotkey registration path failed", exception);
            sanitized.HotkeyEnabled = false;
            _hotkeyManager.Unregister();
        }

        _settings = sanitized;
        _duckingEngine.UpdateSettings(_settings);
        _timer.Interval = Math.Max(15, Math.Min(sanitized.PollIntervalMs, 100));

        if (saveToDisk)
        {
            try
            {
                _settings.Save(_settingsPath);
                AppLogger.Info("Settings saved");
            }
            catch (Exception exception)
            {
                AppLogger.Error("Saving settings failed", exception);
                _trayIcon.ShowBalloonTip(2000, "Spoty Duck", "Settings could not be saved. See log for details.", ToolTipIcon.Warning);
            }
        }

        if (_settings.DuckingEnabled)
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
            _duckingEngine.RestoreImmediately(_spotifyController);
        }

        _duckingToggleMenuItem.Text = _settings.DuckingEnabled ? "Pause ducking" : "Resume ducking";
        _duckingToggleMenuItem.Checked = _settings.DuckingEnabled;
        _gamingModeMenuItem.Text = _settings.GamingModeEnabled ? "Gaming mode enabled" : "Gaming mode disabled";
        _gamingModeMenuItem.Checked = _settings.GamingModeEnabled;
        _startupMenuItem.Text = _settings.StartWithWindows ? "Launch at Windows sign-in" : "Do not launch at Windows sign-in";
        _startupMenuItem.Checked = _settings.StartWithWindows;
        _hotkeyStatusMenuItem.Text = BuildHotkeyStatusText(_settings, hotkeyRegistered);
        _statusMenuItem.Text = BuildStatusText(_settings, shouldDuck: false, _spotifyController.IsAvailable);
        _trayIcon.Text = BuildTrayText(_settings);
        UpdatePresetMenuChecks();
    }

    private void UpdatePresetMenuChecks()
    {
        _defaultPresetMenuItem.Checked = _settings.SelectedPreset == BuiltInPreset.Default;
        _gamingPresetMenuItem.Checked = _settings.SelectedPreset == BuiltInPreset.Gaming;
        _voiceChatPresetMenuItem.Checked = _settings.SelectedPreset == BuiltInPreset.VoiceChat;
        _streamingPresetMenuItem.Checked = _settings.SelectedPreset == BuiltInPreset.Streaming;
        _profileMenuItem.Text = $"Profiles ({GetPresetDisplayName(_settings.SelectedPreset)})";
    }

    private static string BuildTrayText(AppSettings settings)
    {
        var presetSuffix = settings.SelectedPreset == BuiltInPreset.Custom
            ? "Custom"
            : GetPresetDisplayName(settings.SelectedPreset);

        return settings.DuckingEnabled
            ? $"Spoty Duck - {presetSuffix}"
            : $"Spoty Duck - Paused ({presetSuffix})";
    }

    private static string BuildStatusText(AppSettings settings, bool shouldDuck, bool spotifyAvailable)
    {
        var presetText = settings.SelectedPreset == BuiltInPreset.Custom
            ? "Custom"
            : GetPresetDisplayName(settings.SelectedPreset);

        if (!settings.DuckingEnabled)
        {
            return $"Status: Paused - {presetText}";
        }

        if (!spotifyAvailable)
        {
            return $"Status: Waiting for Spotify - {presetText}";
        }

        return shouldDuck ? $"Status: Ducking active - {presetText}" : $"Status: Monitoring audio - {presetText}";
    }

    private static string BuildHotkeyStatusText(AppSettings settings, bool hotkeyRegistered)
    {
        if (!settings.HotkeyEnabled)
        {
            return "Hotkey: Disabled";
        }

        if (!hotkeyRegistered)
        {
            return "Hotkey: Registration failed";
        }

        return $"Hotkey: {FormatHotkey(settings.HotkeyModifiers, settings.HotkeyKey)}";
    }

    private static string GetPresetDisplayName(BuiltInPreset preset)
    {
        return preset switch
        {
            BuiltInPreset.Default => "Default",
            BuiltInPreset.Gaming => "Gaming",
            BuiltInPreset.VoiceChat => "Voice Chat",
            BuiltInPreset.Streaming => "Streaming",
            _ => "Custom"
        };
    }

    private static string FormatHotkey(Keys modifiers, Keys key)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(Keys.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(Keys.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(Keys.Shift))
        {
            parts.Add("Shift");
        }

        parts.Add((key & Keys.KeyCode).ToString());

        return string.Join(" + ", parts);
    }

    private void Exit(object? sender, EventArgs e)
    {
        DisposeResources();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeResources();
        }

        base.Dispose(disposing);
    }

    private void DisposeResources()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _timer.Stop();
        }
        catch (Exception exception)
        {
            AppLogger.Error("Stopping timer during dispose failed", exception);
        }

        try
        {
            _duckingEngine.RestoreImmediately(_spotifyController);
        }
        catch (Exception exception)
        {
            AppLogger.Error("Restoring Spotify volume during dispose failed", exception);
        }

        try
        {
            _hotkeyManager.Dispose();
        }
        catch (Exception exception)
        {
            AppLogger.Error("Disposing hotkey manager failed", exception);
        }

        try
        {
            _trayIcon.Visible = false;
            _timer.Dispose();
            _trayIcon.Dispose();
            _audioMonitor.Dispose();
        }
        catch (Exception exception)
        {
            AppLogger.Error("Disposing tray resources failed", exception);
        }

        AppLogger.Info("Tray context disposed");
    }
}


