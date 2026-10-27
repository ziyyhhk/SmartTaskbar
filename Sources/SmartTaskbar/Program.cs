namespace SmartTaskbar
{
    public static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        ///     Pass --tray (used by the Windows startup entry) to start in the tray without opening settings.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            // Use a mutex to ensure single instance
            using (new Mutex(true, "{959d3545-aa5c-42a8-a327-6e2c079daa94}", out var createNew))
            {
                if (!createNew) return;

                ApplicationConfiguration.Initialize();

                bool startInTray = args.Any(a => string.Equals(a, "--tray", StringComparison.OrdinalIgnoreCase));

                // Start a tray instead of a WinForm to reduce memory usage
                Application.Run(new SystemTray(showSettingsOnLaunch: !startInTray));
            }
        }
    }
}
