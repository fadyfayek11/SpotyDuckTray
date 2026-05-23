using Microsoft.Win32;

namespace SpotyDuckTray;

public sealed class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "SpotyDuckTray";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var currentValue = key?.GetValue(AppName) as string;
            return string.Equals(currentValue, BuildCommand(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            AppLogger.Error("Reading startup registration failed", exception);
            return false;
        }
    }

    public bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

            if (key is null)
            {
                AppLogger.Warning("Startup registry key could not be opened or created");
                return false;
            }

            if (enabled)
            {
                key.SetValue(AppName, BuildCommand());
                AppLogger.Info("Startup registration enabled");
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
                AppLogger.Info("Startup registration disabled");
            }

            return true;
        }
        catch (Exception exception)
        {
            AppLogger.Error($"Updating startup registration to {(enabled ? "enabled" : "disabled")} failed", exception);
            return false;
        }
    }

    public bool Sync(bool desiredState)
    {
        var currentState = IsEnabled();
        if (currentState == desiredState)
        {
            return true;
        }

        var updated = SetEnabled(desiredState);
        if (!updated)
        {
            AppLogger.Warning($"Startup sync did not reach desired state: {desiredState}");
        }

        return updated;
    }

    private static string BuildCommand()
    {
        var executablePath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            executablePath = Application.ExecutablePath;
        }

        return $"\"{executablePath}\"";
    }
}


