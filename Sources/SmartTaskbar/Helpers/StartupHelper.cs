using Microsoft.Win32;

namespace SmartTaskbar
{
    internal static class StartupHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SmartTaskbar";

        public static bool IsRunAtStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                var value = key?.GetValue(AppName) as string;
                return !string.IsNullOrEmpty(value);
            }
            catch
            {
                return false;
            }
        }

        public static void SetRunAtStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key is null) return;

                if (enable)
                {
                    var exePath = Application.ExecutablePath;
                    if (!string.IsNullOrEmpty(exePath))
                        key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch
            {
                // Ignore registry access failures
            }
        }
    }
}
