namespace SpotyDuckTray;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                AppLogger.CaptureUnhandledException("AppDomain", exception);
            }
        };

        Application.ThreadException += (_, args) =>
        {
            AppLogger.CaptureUnhandledException("WindowsForms", args.Exception);
            MessageBox.Show(
                "An unexpected error occurred. The application will continue if possible.\n\nSee logs\\spotyduck.log for details.",
                "Spoty Duck Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        };

        try
        {
            AppLogger.Info("Application starting");
            ApplicationConfiguration.Initialize();
            Application.Run(new TrayAppContext());
            AppLogger.Info("Application stopped");
        }
        catch (Exception exception)
        {
            AppLogger.CaptureUnhandledException("Main", exception);
            MessageBox.Show(
                "A fatal error occurred and the application must close.\n\nSee logs\\spotyduck.log for details.",
                "Spoty Duck Fatal Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

// Made with Bob
