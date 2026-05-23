using System.Text.Json;
using System.Windows.Forms;

namespace SpotyDuckTray;

public sealed class AppSettings
{
    public float DuckLevel { get; set; } = 0.15f;
    public int AttackMs { get; set; } = 200;
    public int ReleaseMs { get; set; } = 600;
    public float Threshold { get; set; } = 0.02f;
    public int PollIntervalMs { get; set; } = 100;
    public bool DuckingEnabled { get; set; } = true;
    public bool GamingModeEnabled { get; set; }
    public bool StartWithWindows { get; set; }
    public float GamingDuckLevel { get; set; } = 0.60f;
    public int FadeDurationMs { get; set; } = 120;
    public AppRuleMode AppRuleMode { get; set; } = AppRuleMode.AllExceptSpotify;
    public List<AppRule> AppRules { get; set; } = [];
    public bool HotkeyEnabled { get; set; }
    public Keys HotkeyModifiers { get; set; } = Keys.Control | Keys.Shift;
    public Keys HotkeyKey { get; set; } = Keys.D;
    public BuiltInPreset SelectedPreset { get; set; } = BuiltInPreset.Custom;

    public static AppSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(path);
            var fileSettings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return Sanitize(fileSettings);
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(string path)
    {
        var sanitized = Sanitize(this);
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(sanitized, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
        CopyFrom(sanitized);
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            DuckLevel = DuckLevel,
            AttackMs = AttackMs,
            ReleaseMs = ReleaseMs,
            Threshold = Threshold,
            PollIntervalMs = PollIntervalMs,
            DuckingEnabled = DuckingEnabled,
            GamingModeEnabled = GamingModeEnabled,
            StartWithWindows = StartWithWindows,
            GamingDuckLevel = GamingDuckLevel,
            FadeDurationMs = FadeDurationMs,
            AppRuleMode = AppRuleMode,
            AppRules = AppRules.Select(static rule => rule.Clone()).ToList(),
            HotkeyEnabled = HotkeyEnabled,
            HotkeyModifiers = HotkeyModifiers,
            HotkeyKey = HotkeyKey,
            SelectedPreset = SelectedPreset
        };
    }

    public void CopyFrom(AppSettings source)
    {
        DuckLevel = source.DuckLevel;
        AttackMs = source.AttackMs;
        ReleaseMs = source.ReleaseMs;
        Threshold = source.Threshold;
        PollIntervalMs = source.PollIntervalMs;
        DuckingEnabled = source.DuckingEnabled;
        GamingModeEnabled = source.GamingModeEnabled;
        StartWithWindows = source.StartWithWindows;
        GamingDuckLevel = source.GamingDuckLevel;
        FadeDurationMs = source.FadeDurationMs;
        AppRuleMode = source.AppRuleMode;
        AppRules = source.AppRules.Select(static rule => rule.Clone()).ToList();
        HotkeyEnabled = source.HotkeyEnabled;
        HotkeyModifiers = source.HotkeyModifiers;
        HotkeyKey = source.HotkeyKey;
        SelectedPreset = source.SelectedPreset;
    }

    public static AppSettings Sanitize(AppSettings? source)
    {
        source ??= new AppSettings();

        var appRules = (source.AppRules ?? [])
            .Where(static rule => !string.IsNullOrWhiteSpace(rule.ProcessName))
            .Select(static rule => new AppRule
            {
                ProcessName = NormalizeProcessName(rule.ProcessName),
                Enabled = rule.Enabled
            })
            .GroupBy(static rule => rule.ProcessName, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First().Clone())
            .OrderBy(static rule => rule.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AppSettings
        {
            DuckLevel = Clamp(source.DuckLevel, 0.10f, 0.30f, 0.15f),
            AttackMs = Clamp(source.AttackMs, 50, 2000, 200),
            ReleaseMs = Clamp(source.ReleaseMs, 100, 5000, 600),
            Threshold = Clamp(source.Threshold, 0.001f, 0.10f, 0.02f),
            PollIntervalMs = Clamp(source.PollIntervalMs, 50, 500, 100),
            DuckingEnabled = source.DuckingEnabled,
            GamingModeEnabled = source.GamingModeEnabled,
            StartWithWindows = source.StartWithWindows,
            GamingDuckLevel = Clamp(source.GamingDuckLevel, 0.50f, 0.70f, 0.60f),
            FadeDurationMs = Clamp(source.FadeDurationMs, 0, 5000, 120),
            AppRuleMode = Enum.IsDefined(source.AppRuleMode) ? source.AppRuleMode : AppRuleMode.AllExceptSpotify,
            AppRules = appRules,
            HotkeyEnabled = source.HotkeyEnabled,
            HotkeyModifiers = SanitizeHotkeyModifiers(source.HotkeyModifiers),
            HotkeyKey = SanitizeHotkeyKey(source.HotkeyKey),
            SelectedPreset = Enum.IsDefined(source.SelectedPreset) ? source.SelectedPreset : BuiltInPreset.Custom
        };
    }

    private static Keys SanitizeHotkeyModifiers(Keys value)
    {
        var modifiers = value & Keys.Modifiers;
        var allowed = new[]
        {
            Keys.Control,
            Keys.Alt,
            Keys.Shift,
            Keys.Control | Keys.Shift,
            Keys.Control | Keys.Alt,
            Keys.Alt | Keys.Shift,
            Keys.Control | Keys.Alt | Keys.Shift
        };

        return allowed.Contains(modifiers) ? modifiers : Keys.Control | Keys.Shift;
    }

    private static Keys SanitizeHotkeyKey(Keys value)
    {
        var key = value & Keys.KeyCode;
        if (key >= Keys.A && key <= Keys.Z)
        {
            return key;
        }

        if (key >= Keys.F1 && key <= Keys.F12)
        {
            return key;
        }

        return Keys.D;
    }

    private static string NormalizeProcessName(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static float Clamp(float value, float min, float max, float fallback)
    {
        if (value <= 0)
        {
            return fallback;
        }

        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static int Clamp(int value, int min, int max, int fallback)
    {
        if (value < 0)
        {
            return fallback;
        }

        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
    public AppSettings ApplyPreset(BuiltInPreset preset)
    {
        var updated = Clone();
        updated.SelectedPreset = preset;

        switch (preset)
        {
            case BuiltInPreset.Default:
                updated.DuckLevel = 0.15f;
                updated.AttackMs = 200;
                updated.ReleaseMs = 600;
                updated.Threshold = 0.02f;
                updated.PollIntervalMs = 100;
                updated.GamingModeEnabled = false;
                updated.GamingDuckLevel = 0.60f;
                updated.FadeDurationMs = 120;
                updated.AppRuleMode = AppRuleMode.AllExceptSpotify;
                break;
            case BuiltInPreset.Gaming:
                updated.DuckLevel = 0.20f;
                updated.AttackMs = 120;
                updated.ReleaseMs = 450;
                updated.Threshold = 0.025f;
                updated.PollIntervalMs = 75;
                updated.GamingModeEnabled = true;
                updated.GamingDuckLevel = 0.65f;
                updated.FadeDurationMs = 80;
                updated.AppRuleMode = AppRuleMode.AllExceptSpotify;
                break;
            case BuiltInPreset.VoiceChat:
                updated.DuckLevel = 0.12f;
                updated.AttackMs = 100;
                updated.ReleaseMs = 700;
                updated.Threshold = 0.015f;
                updated.PollIntervalMs = 75;
                updated.GamingModeEnabled = false;
                updated.GamingDuckLevel = 0.60f;
                updated.FadeDurationMs = 100;
                updated.AppRuleMode = AppRuleMode.Whitelist;
                break;
            case BuiltInPreset.Streaming:
                updated.DuckLevel = 0.18f;
                updated.AttackMs = 180;
                updated.ReleaseMs = 800;
                updated.Threshold = 0.02f;
                updated.PollIntervalMs = 100;
                updated.GamingModeEnabled = false;
                updated.GamingDuckLevel = 0.60f;
                updated.FadeDurationMs = 150;
                updated.AppRuleMode = AppRuleMode.Blacklist;
                break;
            case BuiltInPreset.Custom:
            default:
                break;
        }

        return Sanitize(updated);
    }
}

public enum BuiltInPreset
{
    Custom = 0,
    Default = 1,
    Gaming = 2,
    VoiceChat = 3,
    Streaming = 4
}

public enum AppRuleMode
{
    AllExceptSpotify = 0,
    Whitelist = 1,
    Blacklist = 2
}

public sealed class AppRule
{
    public string ProcessName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    public AppRule Clone()
    {
        return new AppRule
        {
            ProcessName = ProcessName,
            Enabled = Enabled
        };
    }
}

// Made with Bob
