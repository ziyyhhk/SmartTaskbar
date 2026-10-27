using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace SmartTaskbar
{
    /// <summary>
    ///     Settings window styled after Windows 11 Settings: dark surface, rounded cards,
    ///     toggle switches and top tabs with an accent underline.
    ///     Closing the window only hides it, so the tray icon keeps running.
    /// </summary>
    internal sealed class SettingsForm : Form
    {
        private const int Inset = 32;
        private const int TabTop = 96;
        private const int TabHeight = 44;
        private const int PageTop = TabTop + TabHeight;
        private const int CardTop = 40;
        private const int CardGap = 16;

        // DWM attributes (Windows 10 1809+ / Windows 11). Unsupported ones are ignored.
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeLegacy = 19;
        private const int DwmwaWindowCornerPreference = 33;
        private const int DwmwaCaptionColor = 35;
        private const int DwmwcpRound = 2;

        private static readonly string[] TabNames = { "Main", "Animation", "Style", "Startup" };

        private readonly Action<bool> _setAutoMode;
        private readonly Label _lblStatus;
        private readonly TabStrip _tabs;
        private readonly Panel[] _pages;

        private ToggleSwitch _togAutoMain = null!;
        private ToggleSwitch _togAutoStyle = null!;
        private ToggleSwitch _togStartupMain = null!;
        private ToggleSwitch _togStartupPage = null!;
        private ToggleSwitch _togAnimation = null!;
        private ToggleSwitch _togShowOnExit = null!;

        // True while the form itself is changing toggle state, so handlers do not write back.
        private bool _suppress;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        /// <param name="setAutoMode">
        ///     Applies Auto Mode the same way the tray menu does (it owns the taskbar side effects).
        /// </param>
        public SettingsForm(Action<bool> setAutoMode)
        {
            _setAutoMode = setAutoMode;

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "SmartTaskbar";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(760, 480);
            BackColor = Win11Theme.Background;
            ForeColor = Win11Theme.TextPrimary;
            Font = Win11Theme.BodyFont;
            ShowInTaskbar = true;
            DoubleBuffered = true;

            try
            {
                Icon = Fun.IsLightTheme() ? IconResource.Logo_Black : IconResource.Logo_White;
            }
            catch { }

            var title = new Label
            {
                Text = "SmartTaskbar",
                Font = Win11Theme.TitleFont,
                ForeColor = Win11Theme.TextPrimary,
                BackColor = Win11Theme.Background,
                AutoSize = true,
                Location = new Point(Inset, 20)
            };

            _lblStatus = new Label
            {
                Font = Win11Theme.BodyFont,
                ForeColor = Win11Theme.TextSecondary,
                BackColor = Win11Theme.Background,
                AutoSize = true,
                Location = new Point(Inset, 62)
            };

            _tabs = new TabStrip
            {
                Location = new Point(0, TabTop),
                Size = new Size(ClientSize.Width, TabHeight),
                LeftInset = Inset,
                AccessibleName = "Settings sections"
            };
            _tabs.SetItems(TabNames);
            _tabs.SelectedIndexChanged += (_, _) => ShowPage(_tabs.SelectedIndex);

            _pages = new[]
            {
                BuildMainPage(),
                BuildAnimationPage(),
                BuildStylePage(),
                BuildStartupPage()
            };

            Controls.Add(title);
            Controls.Add(_lblStatus);
            Controls.Add(_tabs);
            foreach (var page in _pages)
                Controls.Add(page);

            ShowPage(_tabs.SelectedIndex);
            LoadSettings();

            FormClosing += SettingsForm_FormClosing;
        }

        // ===== Pages =====

        private Panel BuildMainPage()
        {
            var page = CreatePage();
            page.Controls.Add(CreateSectionLabel("Taskbar", Inset, 8));

            var card = CreateCard(page, CardTop);
            _togAutoMain = card.AddRow(
                "Auto Mode",
                "Hide the taskbar automatically when the focused window overlaps it.");
            _togStartupMain = card.AddRow(
                "Run at Windows startup",
                "Start SmartTaskbar automatically when you sign in to Windows.");

            _togAutoMain.CheckedChanged += (_, _) => OnAutoModeToggled(_togAutoMain.Checked);
            _togStartupMain.CheckedChanged += (_, _) => OnStartupToggled(_togStartupMain.Checked);

            page.Controls.Add(CreateInfoBar(
                "Closing this window keeps SmartTaskbar running in the system tray. Double-click the tray icon to reopen it.",
                Inset, card.Bottom + CardGap, card.Width));

            return page;
        }

        private Panel BuildAnimationPage()
        {
            var page = CreatePage();
            page.Controls.Add(CreateSectionLabel("Motion", Inset, 8));

            var card = CreateCard(page, CardTop);
            _togAnimation = card.AddRow(
                "Taskbar animation",
                "Turn the Windows taskbar animation effects on or off.");

            _togAnimation.CheckedChanged += (_, _) => OnAnimationToggled(_togAnimation.Checked);

            return page;
        }

        private Panel BuildStylePage()
        {
            var page = CreatePage();
            page.Controls.Add(CreateSectionLabel("Behavior", Inset, 8));

            var card = CreateCard(page, CardTop);
            _togAutoStyle = card.AddRow(
                "Auto Mode",
                "Hide the taskbar when the focused window intersects it.");
            _togShowOnExit = card.AddRow(
                "Show taskbar on exit",
                "Restore the taskbar when SmartTaskbar exits.");

            _togAutoStyle.CheckedChanged += (_, _) => OnAutoModeToggled(_togAutoStyle.Checked);
            _togShowOnExit.CheckedChanged += (_, _) => OnShowOnExitToggled(_togShowOnExit.Checked);

            return page;
        }

        private Panel BuildStartupPage()
        {
            var page = CreatePage();
            page.Controls.Add(CreateSectionLabel("Launch", Inset, 8));

            var card = CreateCard(page, CardTop);
            _togStartupPage = card.AddRow(
                "Run at Windows startup",
                "Start SmartTaskbar automatically when you sign in.");

            _togStartupPage.CheckedChanged += (_, _) => OnStartupToggled(_togStartupPage.Checked);

            page.Controls.Add(CreateInfoBar(
                "Stored in the current-user Run key (HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run). No administrator rights are needed.",
                Inset, card.Bottom + CardGap, card.Width));

            return page;
        }

        private Panel CreatePage() => new Panel
        {
            Location = new Point(0, PageTop),
            Size = new Size(ClientSize.Width, ClientSize.Height - PageTop),
            BackColor = Win11Theme.Background,
            Visible = false
        };

        private static SettingsCard CreateCard(Panel page, int top)
        {
            var card = new SettingsCard
            {
                Location = new Point(Inset, top),
                Width = page.ClientSize.Width - Inset * 2
            };
            page.Controls.Add(card);
            return card;
        }

        private static Label CreateSectionLabel(string text, int x, int y) => new Label
        {
            Text = text,
            Font = Win11Theme.SectionFont,
            ForeColor = Win11Theme.TextPrimary,
            BackColor = Win11Theme.Background,
            AutoSize = true,
            Location = new Point(x, y)
        };

        /// <summary>
        ///     Rounded info bar with an accent info glyph, in the style of Windows 11 info bars.
        /// </summary>
        private static Panel CreateInfoBar(string message, int x, int y, int width)
        {
            const int IconWidth = 20;
            const int IconLeft = 16;
            const int TextLeft = 44;
            const int TextRight = 16;
            const int PadY = 12;

            int textWidth = width - TextLeft - TextRight;
            int textHeight = TextRenderer.MeasureText(
                message,
                Win11Theme.BodyFont,
                new Size(textWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;

            int height = Math.Max(48, textHeight + PadY * 2);

            var bar = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Win11Theme.Background
            };

            bar.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = Win11Theme.RoundedRectangle(new RectangleF(0F, 0F, bar.Width - 1F, bar.Height - 1F), 8F);
                using var brush = new SolidBrush(Win11Theme.Surface);
                e.Graphics.FillPath(brush, path);
            };

            bar.Controls.Add(new Label
            {
                Text = "\uE946", // Segoe MDL2 Assets: Info
                Font = Win11Theme.IconFont,
                ForeColor = Win11Theme.Accent,
                BackColor = Win11Theme.Surface,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(IconLeft, 0),
                Size = new Size(IconWidth, height)
            });

            bar.Controls.Add(new Label
            {
                Text = message,
                Font = Win11Theme.BodyFont,
                ForeColor = Win11Theme.TextPrimary,
                BackColor = Win11Theme.Surface,
                Location = new Point(TextLeft, PadY),
                Size = new Size(textWidth, textHeight)
            });

            return bar;
        }

        private void ShowPage(int index)
        {
            for (int i = 0; i < _pages.Length; i++)
                _pages[i].Visible = i == index;
        }

        // ===== Change handlers =====

        private void OnAutoModeToggled(bool enabled)
        {
            if (_suppress) return;

            try
            {
                _setAutoMode(enabled);
            }
            catch
            {
                // The tray is responsible for the taskbar side effects; fall through and re-sync.
            }

            SetAutoModeUi(UserSettings.AutoModeType == AutoModeType.Auto);
        }

        private void OnStartupToggled(bool enabled)
        {
            if (_suppress) return;

            bool written = StartupHelper.SetRunAtStartup(enabled);
            SetStartupUi(written ? enabled : StartupHelper.IsRunAtStartup());
        }

        private void OnAnimationToggled(bool enabled)
        {
            if (_suppress) return;

            if (enabled != Fun.IsEnableTaskbarAnimation())
                Fun.ChangeTaskbarAnimation();

            bool actual = Fun.IsEnableTaskbarAnimation();
            WithSuppressed(() => _togAnimation.Checked = actual);
        }

        private void OnShowOnExitToggled(bool enabled)
        {
            if (_suppress) return;

            UserSettings.ShowTaskbarWhenExit = enabled;
        }

        // ===== State sync =====

        private void SetAutoModeUi(bool autoOn)
        {
            WithSuppressed(() =>
            {
                _togAutoMain.Checked = autoOn;
                _togAutoStyle.Checked = autoOn;
            });
            UpdateStatus(autoOn);
        }

        private void SetStartupUi(bool startupOn)
        {
            WithSuppressed(() =>
            {
                _togStartupMain.Checked = startupOn;
                _togStartupPage.Checked = startupOn;
            });
        }

        private void UpdateStatus(bool autoOn)
        {
            _lblStatus.Text = autoOn ? "Auto Mode is on" : "Auto Mode is off";
            _lblStatus.ForeColor = autoOn ? Win11Theme.Accent : Win11Theme.TextSecondary;
        }

        /// <summary>
        ///     Reads the current settings (registry, UserSettings, system animation) into the controls
        ///     without triggering any writes.
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                bool autoOn = UserSettings.AutoModeType == AutoModeType.Auto;
                bool startupOn = StartupHelper.IsRunAtStartup();
                bool animationOn = Fun.IsEnableTaskbarAnimation();
                bool showOnExit = UserSettings.ShowTaskbarWhenExit;

                WithSuppressed(() =>
                {
                    _togAutoMain.Checked = autoOn;
                    _togAutoStyle.Checked = autoOn;
                    _togStartupMain.Checked = startupOn;
                    _togStartupPage.Checked = startupOn;
                    _togAnimation.Checked = animationOn;
                    _togShowOnExit.Checked = showOnExit;
                });

                UpdateStatus(autoOn);
            }
            catch
            {
                // Keep the window usable even if a system query fails.
            }
        }

        private void WithSuppressed(Action action)
        {
            bool previous = _suppress;
            _suppress = true;
            try
            {
                action();
            }
            finally
            {
                _suppress = previous;
            }
        }

        /// <summary>
        ///     Called by the tray when a setting changes while this window is open.
        /// </summary>
        public void RefreshState() => LoadSettings();

        // ===== Window behavior =====

        private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyWindowChrome();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadSettings();
            BringToFront();
            Activate();
        }

        /// <summary>
        ///     Dark title bar, rounded window corners and matching caption color on Windows 11.
        ///     Each attribute is optional, so older builds simply keep the default chrome.
        /// </summary>
        private void ApplyWindowChrome()
        {
            try
            {
                int dark = 1;
                if (DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int)) != 0)
                    DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkModeLegacy, ref dark, sizeof(int));

                int corner = DwmwcpRound;
                DwmSetWindowAttribute(Handle, DwmwaWindowCornerPreference, ref corner, sizeof(int));

                int caption = ColorTranslator.ToWin32(Win11Theme.Background);
                DwmSetWindowAttribute(Handle, DwmwaCaptionColor, ref caption, sizeof(int));
            }
            catch
            {
                // Non-fatal: the window still works with the default title bar.
            }
        }
    }
}
