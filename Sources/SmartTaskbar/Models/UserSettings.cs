using Microsoft.Win32;

namespace SmartTaskbar
{
    public class UserSettings
    {
        private const string RegPath = @"Software\SmartTaskbar";
        private static UserConfiguration _userConfiguration;

        static UserSettings()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegPath, false);
                var autoMode = key?.GetValue(nameof(UserConfiguration.AutoModeType)) as string;
                var showExit = key?.GetValue(nameof(UserConfiguration.ShowTaskbarWhenExit));
                var enabled = key?.GetValue(nameof(UserConfiguration.Enabled));

                _userConfiguration = new UserConfiguration
                {
                    AutoModeType = autoMode == nameof(AutoModeType.None)
                        ? AutoModeType.None
                        : AutoModeType.Auto,
                    ShowTaskbarWhenExit = showExit is int i
                        ? i != 0
                        : showExit is bool b
                            ? b
                            : true,
                    Enabled = enabled is int e ? e != 0 : true
                };
            }
            catch
            {
                _userConfiguration = new UserConfiguration
                {
                    AutoModeType = AutoModeType.Auto,
                    ShowTaskbarWhenExit = true,
                    Enabled = true
                };
            }
        }

        public static bool Enabled
        {
            get => _userConfiguration.Enabled;
            set
            {
                if (value == _userConfiguration.Enabled)
                    return;

                _userConfiguration.Enabled = value;
                WriteValue(nameof(UserConfiguration.Enabled), value ? 1 : 0);
            }
        }

        public static AutoModeType AutoModeType
        {
            get => _userConfiguration.AutoModeType;
            set
            {
                if (value == _userConfiguration.AutoModeType)
                    return;

                _userConfiguration.AutoModeType = value;
                WriteValue(nameof(UserConfiguration.AutoModeType), value.ToString());
            }
        }

        public static bool ShowTaskbarWhenExit
        {
            get => _userConfiguration.ShowTaskbarWhenExit;
            set
            {
                if (value == _userConfiguration.ShowTaskbarWhenExit)
                    return;

                _userConfiguration.ShowTaskbarWhenExit = value;
                WriteValue(nameof(UserConfiguration.ShowTaskbarWhenExit), value ? 1 : 0);
            }
        }

        private static void WriteValue(string name, object value)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegPath);
                key?.SetValue(name, value);
            }
            catch
            {
                // ignore write failures
            }
        }
    }
}
