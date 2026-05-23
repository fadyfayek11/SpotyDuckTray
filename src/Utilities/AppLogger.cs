namespace SpotyDuckTray;

public static class AppLogger
{
    private static readonly object SyncRoot = new();
    private static readonly string LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
    private static readonly string LogPath = Path.Combine(LogDirectory, "spotyduck.log");

    public static void Info(string message)
    {
        Write("INFO", message, null);
    }

    public static void Warning(string message)
    {
        Write("WARN", message, null);
    }

    public static void Error(string message, Exception? exception = null)
    {
        Write("ERROR", message, exception);
    }

    public static void CaptureUnhandledException(string source, Exception exception)
    {
        Write("FATAL", $"Unhandled exception from {source}", exception);
    }

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            var lines = new List<string>
            {
                $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{level}] {message}"
            };

            if (exception is not null)
            {
                lines.Add(exception.ToString());
            }

            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllLines(LogPath, lines);
            }
        }
        catch
        {
        }
    }
}


