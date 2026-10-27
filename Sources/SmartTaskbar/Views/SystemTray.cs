using System.ComponentModel;
using System.Diagnostics;
using Windows.System;
using Windows.UI.ViewManagement;
using SmartTaskbar.Languages;

namespace SmartTaskbar
{
    internal class SystemTray : ApplicationContext
    {
        private const int TrayTolerance = 4;
        private readonly ToolStripMenuItem _animationInBar;
        private readonly ToolStripMenuItem _autoMode;
        private readonly ToolStripMenuItem _settings;

        private readonly Container _container = new();
        private readonly ContextMenuStrip _contextMenuStrip;

        private readonly Engine _engine;
        private readonly ToolStripMenuItem _exit;
        private readonly NotifyIcon _notifyIcon;
        private readonly ResourceCulture _resourceCulture = new();
        private readonly ToolStripMenuItem _showBarOnExit;
        private SettingsForm? _settingsForm;
        private readonly TaskbarCenterer _centerer = new();

        public SystemTray(bool showSettingsOnLaunch = true)
        {
            #region Initialization

            _engine = new Engine(_container);

            var font = new Font("Segoe UI", 10.5F);

            var about = new ToolStripMenuItem(_resourceCulture.GetString(LangName.About))
            {
                Font = font
            };
            _settings = new ToolStripMenuItem("Settings")
            {
                Font = font
            };
            _animationInBar = new ToolStripMenuItem(_resourceCulture.GetString(LangName.Animation))
            {
                Font = font
            };
            _showBarOnExit = new ToolStripMenuItem(_resourceCulture.GetString(LangName.ShowBarOnExit))
            {
                Font = font
            };
            _autoMode = new ToolStripMenuItem(_resourceCulture.GetString(LangName.Auto))
            {
                Font = font
            };
            _exit = new ToolStripMenuItem(_resourceCulture.GetString(LangName.Exit))
            {
                Font = font
            };

            _contextMenuStrip = new ContextMenuStrip(_container)
            {
                Renderer = new Win11Renderer()
            };

            _contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                about,
                _settings,
                new ToolStripSeparator(),
                _animationInBar,
                new ToolStripSeparator(),
                _autoMode,
                new ToolStripSeparator(),
                _showBarOnExit,
                _exit
            });

            _notifyIcon = new NotifyIcon(_container)
            {
                Text = Application.ProductName,
                Icon = Fun.IsLightTheme() ? IconResource.Logo_Black : IconResource.Logo_White,
                Visible = true
            };

            #endregion

            #region Load Event

            about.Click += AboutOnClick;
            _settings.Click += SettingsOnClick;
            _animationInBar.Click += AnimationInBarOnClick;
            _showBarOnExit.Click += ShowBarOnExitOnClick;
            _autoMode.Click += AutoModeOnClick;
            _exit.Click += ExitOnClick;

            _notifyIcon.MouseClick += NotifyIconOnMouseClick;
            _notifyIcon.MouseDoubleClick += NotifyIconOnMouseDoubleClick;

            Fun.UiSettings.ColorValuesChanged += UISettingsOnColorValuesChanged;
            Application.ApplicationExit += Application_ApplicationExit;

            #endregion

            // Start icon centering if it was enabled last time
            ApplyCentering();

            // Open settings GUI on launch (after tray is ready), unless started with --tray
            if (showSettingsOnLaunch)
            {
                try
                {
                    ShowSettings();
                }
                catch
                {
                    // Never crash the tray if the form fails
                }
            }
        }

        private void SettingsOnClick(object? sender, EventArgs e) => ShowSettings();

        private void ShowSettings()
        {
            try
            {
                if (_settingsForm is null || _settingsForm.IsDisposed)
                {
                    _settingsForm = new SettingsForm(new TaskbarActions(ApplyEnabled, ApplyAutoMode, SetCenter, ApplyCentering));
                }

                if (_settingsForm.Visible)
                {
                    _settingsForm.BringToFront();
                    _settingsForm.Activate();
                    return;
                }

                _settingsForm.Show();
                _settingsForm.BringToFront();
                _settingsForm.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to open settings window:\n" + ex.Message,
                    "SmartTaskbar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void AboutOnClick(object? sender, EventArgs e)
            => _ = Launcher.LaunchUriAsync(new Uri("https://github.com/ChanpleCai/SmartTaskbar"));

        private void UISettingsOnColorValuesChanged(UISettings s, object e)
            => _notifyIcon.Icon = Fun.IsLightTheme() ? IconResource.Logo_Black : IconResource.Logo_White;

        private void NotifyIconOnMouseDoubleClick(object? s, MouseEventArgs e)
        {
            ShowSettings();
        }

        private void NotifyIconOnMouseClick(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            _animationInBar.Checked = Fun.IsEnableTaskbarAnimation();
            _showBarOnExit.Checked = UserSettings.ShowTaskbarWhenExit;
            _autoMode.Checked = UserSettings.AutoModeType == AutoModeType.Auto;

            ShowMenu();

            Fun.SetForegroundWindow(_contextMenuStrip.Handle);
        }

        private void ShowMenu()
        {
            var taskbar = TaskbarHelper.InitTaskbar();

            if (taskbar.Handle == IntPtr.Zero)
                return;

            switch (taskbar.Position)
            {
                case TaskbarPosition.Bottom:
                    if (Cursor.Position.X + _contextMenuStrip.Width > Screen.PrimaryScreen.Bounds.Right)
                        _contextMenuStrip.Show(
                            Screen.PrimaryScreen.Bounds.Right - _contextMenuStrip.Width - TrayTolerance,
                            taskbar.Rect.top - _contextMenuStrip.Height - TrayTolerance);
                    else
                        _contextMenuStrip.Show(Cursor.Position.X - TrayTolerance,
                                               taskbar.Rect.top - _contextMenuStrip.Height - TrayTolerance);
                    break;
                case TaskbarPosition.Left:
                    if (Cursor.Position.Y + _contextMenuStrip.Height > Screen.PrimaryScreen.Bounds.Bottom)
                        _contextMenuStrip.Show(taskbar.Rect.right + TrayTolerance,
                                               Screen.PrimaryScreen.Bounds.Bottom
                                               - _contextMenuStrip.Height
                                               - TrayTolerance);
                    else
                        _contextMenuStrip.Show(taskbar.Rect.right + TrayTolerance,
                                               Cursor.Position.Y - TrayTolerance);
                    break;
                case TaskbarPosition.Right:
                    if (Cursor.Position.Y + _contextMenuStrip.Height > Screen.PrimaryScreen.Bounds.Bottom)
                        _contextMenuStrip.Show(taskbar.Rect.left - TrayTolerance - _contextMenuStrip.Width,
                                               Screen.PrimaryScreen.Bounds.Bottom
                                               - _contextMenuStrip.Height
                                               - TrayTolerance);
                    else
                        _contextMenuStrip.Show(taskbar.Rect.left - TrayTolerance - _contextMenuStrip.Width,
                                               Cursor.Position.Y - TrayTolerance);
                    break;
                case TaskbarPosition.Top:
                    if (Cursor.Position.X + _contextMenuStrip.Width > Screen.PrimaryScreen.Bounds.Right)
                        _contextMenuStrip.Show(
                            Screen.PrimaryScreen.Bounds.Right - _contextMenuStrip.Width - TrayTolerance,
                            taskbar.Rect.bottom + TrayTolerance);
                    else
                        _contextMenuStrip.Show(Cursor.Position.X - TrayTolerance,
                                               taskbar.Rect.bottom + TrayTolerance);
                    break;
            }
        }

        private static void HideBar()
        {
            if (Fun.IsNotAutoHide())
                return;

            var taskbar = TaskbarHelper.InitTaskbar();

            if (taskbar.Handle != IntPtr.Zero)
                taskbar.HideTaskbar();
        }

        private void ExitOnClick(object? s, EventArgs e)
        {
            _centerer.Stop();   // put the taskbar icons back where they were
            if (UserSettings.ShowTaskbarWhenExit)
                Fun.CancelAutoHide();
            else
                HideBar();
            _container?.Dispose();
            Application.Exit();
        }

        private void ShowBarOnExitOnClick(object? s, EventArgs e)
        {
            UserSettings.ShowTaskbarWhenExit = !_showBarOnExit.Checked;
            RefreshSettingsWindow();
        }

        private void AutoModeOnClick(object? s, EventArgs e)
        {
            ApplyAutoMode(!_autoMode.Checked);
            RefreshSettingsWindow();
        }

        /// <summary>
        ///     Single place that turns Auto Mode on or off. Used by the tray menu and the settings window.
        /// </summary>
        private void ApplyAutoMode(bool enabled)
        {
            if (enabled)
            {
                UserSettings.AutoModeType = AutoModeType.Auto;
            }
            else
            {
                UserSettings.AutoModeType = AutoModeType.None;
                HideBar();
            }

            _autoMode.Checked = enabled;
        }

        /// <summary>
        ///     Master switch. Turning it off stops the engine and restores a normal, visible taskbar.
        /// </summary>
        private void ApplyEnabled(bool enabled)
        {
            UserSettings.Enabled = enabled;

            if (!enabled)
                Fun.CancelAutoHide();

            ApplyCentering();
        }

        /// <summary>
        ///     Centering switch. Windows 11 uses its native alignment setting; Windows 10 uses the centering engine.
        /// </summary>
        private void SetCenter(bool enabled)
        {
            if (TaskbarAlignmentHelper.IsSupported)
                TaskbarAlignmentHelper.SetCentered(enabled);
            else
                UserSettings.CenterIcons = enabled;

            ApplyCentering();
        }

        /// <summary>
        ///     Starts or stops the Windows 10 centering engine to match the settings.
        /// </summary>
        private void ApplyCentering()
        {
            bool wanted = !TaskbarAlignmentHelper.IsSupported && UserSettings.Enabled && UserSettings.CenterIcons;

            if (!wanted)
            {
                _centerer.Stop();
                return;
            }

            _centerer.Configure(UserSettings.CenterEasing, UserSettings.CenterDurationMs);
            _centerer.Start();
        }

        private void AnimationInBarOnClick(object? s, EventArgs e)
        {
            _animationInBar.Checked = Fun.ChangeTaskbarAnimation();
            RefreshSettingsWindow();
        }

        private void RefreshSettingsWindow()
        {
            if (_settingsForm is { IsDisposed: false, Visible: true })
                _settingsForm.RefreshState();
        }

        private static async void Application_ApplicationExit(object? sender, EventArgs e)
        {
            await Task.Delay(500);
            Process.GetCurrentProcess().Kill();
        }
    }
}
