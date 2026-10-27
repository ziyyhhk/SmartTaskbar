using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SmartTaskbar
{
    /// <summary>
    ///     Windows 11 "Taskbar alignment" (center or left). Windows 10 has no such setting.
    /// </summary>
    internal static class TaskbarAlignmentHelper
    {
        private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        private const string ValueName = "TaskbarAl";
        private const int Windows11FirstBuild = 22000;

        private const uint WmSettingChange = 0x001A;
        private const uint SmtoAbortIfHung = 0x0002;
        private const uint BroadcastTimeoutMs = 200;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, string lParam,
                                                        uint flags, uint timeout, out IntPtr result);

        public static bool IsSupported => Environment.OSVersion.Version.Build >= Windows11FirstBuild;

        /// <summary>
        ///     True when icons are centered. A missing value means the Windows 11 default (centered).
        /// </summary>
        public static bool IsCentered()
        {
            if (!IsSupported) return false;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(KeyPath, false);
                return key?.GetValue(ValueName) is int value ? value != 0 : true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Writes the alignment and notifies Explorer. Returns false when unsupported or the write failed.
        ///     Explorer may need a sign-out or restart before the icons actually move.
        /// </summary>
        public static bool SetCentered(bool centered)
        {
            if (!IsSupported) return false;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true)
                                ?? Registry.CurrentUser.CreateSubKey(KeyPath);
                if (key is null) return false;

                key.SetValue(ValueName, centered ? 1 : 0, RegistryValueKind.DWord);
            }
            catch
            {
                return false;
            }

            SendMessageTimeout(new IntPtr(0xffff), WmSettingChange, IntPtr.Zero, "TraySettings",
                               SmtoAbortIfHung, BroadcastTimeoutMs, out _);
            return true;
        }
    }
}
