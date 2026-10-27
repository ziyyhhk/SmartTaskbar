using Microsoft.Win32;

namespace SmartTaskbar
{
    /// <summary>
    ///     "Run at Windows startup" via the current-user Run key (HKCU, no administrator rights).
    /// </summary>
    internal static class StartupHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SmartTaskbar";

        public static bool IsRunAtStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                return key?.GetValue(AppName) is string value && value.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Adds or removes the Run entry. Returns true when the registry write succeeded.
        /// </summary>
        public static bool SetRunAtStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key is null) return false;

                if (!enable)
                {
                    key.DeleteValue(AppName, false);
                    return true;
                }

                string exePath = GetExecutablePath();

                // Never record the dotnet host (e.g. when started with "dotnet run"); it would not launch the app.
                if (string.IsNullOrEmpty(exePath)
                    || string.Equals(Path.GetFileNameWithoutExtension(exePath), "dotnet", StringComparison.OrdinalIgnoreCase))
                    return false;

                key.SetValue(AppName, $"\"{exePath}\"");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetExecutablePath()
            => Environment.ProcessPath ?? Application.ExecutablePath;
    }
}
