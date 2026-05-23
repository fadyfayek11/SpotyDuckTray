namespace SpotyDuckTray;

public static class AudioRuleEngine
{
    public static bool ShouldDuck(AppSettings settings, IReadOnlyCollection<AudioSessionInfo> sessions)
    {
        if (sessions.Count == 0)
        {
            return false;
        }

        var activeSessions = sessions.Where(static session => session.IsActive).ToList();
        if (activeSessions.Count == 0)
        {
            return false;
        }

        var enabledRules = settings.AppRules
            .Where(static rule => rule.Enabled)
            .Select(static rule => Normalize(rule.ProcessName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return settings.AppRuleMode switch
        {
            AppRuleMode.AllExceptSpotify => activeSessions.Count > 0,
            AppRuleMode.Whitelist => activeSessions.Any(session => enabledRules.Contains(Normalize(session.ProcessName))),
            AppRuleMode.Blacklist => activeSessions.Any(session => !enabledRules.Contains(Normalize(session.ProcessName))),
            _ => activeSessions.Count > 0
        };
    }

    public static List<string> MergeDetectedApps(IEnumerable<AudioSessionInfo> sessions, IEnumerable<AppRule> existingRules)
    {
        return sessions
            .Select(static session => Normalize(session.ProcessName))
            .Concat(existingRules.Select(static rule => Normalize(rule.ProcessName)))
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Normalize(string? processName)
    {
        return (processName ?? string.Empty).Trim().ToLowerInvariant();
    }
}


