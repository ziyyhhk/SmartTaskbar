using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace SmartTaskbar
{
    /// <summary>
    ///     Settings window in the Windows 11 Settings style. It uses a borderless window with its own
    ///     dark title bar (so it matches the app on Windows 10 too), resizes and maximizes normally,
    ///     and hides instead of closing so the tray keeps running.
    /// </summary>
    internal sealed class SettingsForm : Form
    {
        private enum CaptionButton { None, Minimize, Maximize, Close }

        // ----- Layout -----
        private const int Inset = 32;
        private const int CaptionHeight = 32;
        private const int CaptionButtonWidth = 46;
        private const int TitleTop = CaptionHeight + 16;
        private const int StatusTop = TitleTop + 42;
        private const int TabTop = CaptionHeight + 92;
        private const int TabHeight = 44;
        private const int PageTop = TabTop + TabHeight;
        private const int CardTop = 40;
        private const int CardGap = 16;

        // ----- Win32 -----
        private const int WmNcCalcSize = 0x0083;
        private const int WmNcHitTest = 0x0084;
        private const int WmNcMouseMove = 0x00A0;
        private const int WmNcLButtonDblClk = 0x00A3;

        private const int HtClient = 1;
        private const int HtCaption = 2;
        private const int HtLeft = 10;
        private const int HtRight = 11;
        private const int HtTop = 12;
        private const int HtTopLeft = 13;
        private const int HtTopRight = 14;
        private const int HtBottom = 15;
        private const int HtBottomLeft = 16;
        private const int HtBottomRight = 17;

        private const int ResizeBorder = 6;
        private const int WsThickFrame = 0x00040000;
        private const int WsMinimizeBox = 0x00020000;
        private const int WsMaximizeBox = 0x00010000;
        private const int CsDropShadow = 0x00020000;

        // DWM attributes (Windows 10 1809+ and Windows 11). Unsupported ones are ignored.
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeLegacy = 19;
        private const int DwmwaWindowCornerPreference = 33;
        private const int DwmwcpRound = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private static readonly string[] TabNames = { "Main", "Animation", "Style", "Startup" };

        private readonly TaskbarActions _actions;
        private readonly List<Action> _pageLayouts = new();
        private readonly Label _titleLabel;
        private readonly Label _lblStatus;
        private readonly TabStrip _tabs;
        private readonly Panel[] _pages;

        private bool _layoutReady;
        private bool _suppress;
        private CaptionButton _hoverButton = CaptionButton.None;
        private CaptionButton _pressedButton = CaptionButton.None;

        private ToggleSwitch _togEnabled = null!;
        private ToggleSwitch _togAutoMain = null!;
        private ToggleSwitch _togAutoStyle = null!;
        private ToggleSwitch _togStartupMain = null!;
        private ToggleSwitch _togStartupPage = null!;
        private ToggleSwitch _togCenter = null!;
        private ToggleSwitch _togAnimation = null!;
        private ToggleSwitch _togShowOnExit = null!;
        private ComboBox _easingCombo = null!;
        private ComboBox _speedCombo = null!;

        public SettingsForm(TaskbarActions actions)
        {
            _actions = actions;

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "SmartTaskbar";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(700, 520);
            ClientSize = new Size(800, 620);
            BackColor = Win11Theme.Background;
            ForeColor = Win11Theme.TextPrimary;
            Font = Win11Theme.BodyFont;
            ShowInTaskbar = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);

            try
            {
                Icon = Fun.IsLightTheme() ? IconResource.Logo_Black : IconResource.Logo_White;
            }
            catch { }

            _titleLabel = new Label
            {
                Text = "SmartTaskbar",
                Font = Win11Theme.TitleFont,
                ForeColor = Win11Theme.TextPrimary,
                BackColor = Win11Theme.Background,
                AutoSize = true,
                Location = new Point(Inset, TitleTop)
            };

            _lblStatus = new Label
            {
                Font = Win11Theme.BodyFont,
                ForeColor = Win11Theme.TextSecondary,
                BackColor = Win11Theme.Background,
                AutoSize = true,
                Location = new Point(Inset, StatusTop)
            };

            _tabs = new TabStrip
            {
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

            Controls.Add(_titleLabel);
            Controls.Add(_lblStatus);
            Controls.Add(_tabs);
            foreach (var page in _pages)
                Controls.Add(page);

            _layoutReady = true;
            LayoutContent();
            ShowPage(_tabs.SelectedIndex);
            SyncFromSettings();

            FormClosing += SettingsForm_FormClosing;
        }

        // ===== Pages =====

        private Panel BuildMainPage()
        {
            var page = CreatePage();
            page.Controls.Add(CreateSectionLabel("Taskbar", 8));

            var card = CreateCard(page);
            _togEnabled = card.AddRow(
                "Enable SmartTaskbar",
                "Turn SmartTaskbar's taskbar behavior on or off. When it's off, the taskbar stays visible.");
            _togAutoMain = card.AddRow(
                "Auto Mode",
                "Hide the taskbar automatically when the focused window overlaps it.");
            _togStartupMain = card.AddRow(
                "Run at Windows startup",
                "Start SmartTaskbar in the tray when you sign in to Windows.");
            _togCenter = card.AddRow(
                "Center taskbar icons",
                TaskbarAlignmentHelper.IsSupported
                    ? "Move the Start button and app icons to the middle of the taskbar (Windows 11)."
                    : "Move the taskbar icons to the middle of the screen with the animation picked on the Animation tab. Experimental on Windows 10.");

            _togEnabled.CheckedChanged += (_, _) => OnEnabledToggled(_togEnabled.Checked);
            _togAutoMain.CheckedChanged += (_, _) => OnAutoModeToggled(_togAutoMain.Checked);
            _togStartupMain.CheckedChanged += (_, _) => OnStartupToggled(_togStartupMain.Checked);
            _togCenter.CheckedChanged += (_, _) => OnCenterToggled(_togCenter.Checked);

            var info = new InfoBar(
                "Closing this window keeps SmartTaskbar running in the system tray. Double-click the tray icon to reopen it.");
            page.Controls.Add(info);
            RegisterPageLayout(page, card, info);
            return page;
        }

        private Panel BuildAnimationPage()
        {
            var page = CreatePage();
            page.Controls.Add(CreateSectionLabel("Motion", 8));

            var card = CreateCard(page);
            _togAnimation = card.AddRow(
                "Taskbar animation",
                "Turn the Windows taskbar animation effects on or off.");
            _easingCombo = card.AddChoiceRow(
                "Centering easing",
                "The curve the icons follow when they move to the middle.",
                CenterOptions.EasingNames,
                Array.IndexOf(CenterOptions.Easings, CenterEasing.EaseOut));
            _speedCombo = card.AddChoiceRow(
                "Centering speed",
                "How long the centering animation takes.",
                CenterOptions.SpeedNames,
                1);

            _togAnimation.CheckedChanged += (_, _) => OnAnimationToggled(_togAnimation.Checked);
            _easingCombo.SelectedIndexChanged += (_, _) => OnEasingChanged(_easingCombo.SelectedIndex);
            _speedCombo.SelectedIndexChanged += (_, _) => OnSpeedChanged(_speedCombo.SelectedIndex);

            // Windows 11 centers icons with its own animation, so the custom options only apply on Windows 10.
            bool windows10 = !TaskbarAlignmentHelper.IsSupported;
            _easingCombo.Enabled = windows10;
            _speedCombo.Enabled = windows10;

            var info = new InfoBar(windows10
                ? "Easing and speed apply to the icon centering animation on Windows 10. The taskbar animation switch controls Windows' own animations."
                : "Windows 11 centers icons with its own animation, so the easing and speed options only apply on Windows 10.");
            page.Controls.Add(info);
            RegisterPageLayout(page, card, info);
            return page;
        }

        private Panel BuildStylePage()
        {
            var page = CreatePage();
            page.Controls.Add(CreateSectionLabel("Behavior", 8));

            var card = CreateCard(page);
            _togAutoStyle = card.AddRow(
                "Auto Mode",
                "Hide the taskbar when the focused window intersects it.");
            _togShowOnExit = card.AddRow(
                "Show taskbar on exit",
                "Restore the taskbar when SmartTaskbar exits.");

            _togAutoStyle.CheckedChanged += (_, _) => OnAutoModeToggled(_togAutoStyle.Checked);
            _togShowOnExit.CheckedChanged += (_, _) => OnShowOnExitToggled(_togShowOnExit.Checked);

            RegisterPageLayout(page, card, null);
            return page;
        }

        private Panel BuildStartupPage()
        {
            var page = CreatePage();
            page.Controls.Add(CreateSectionLabel("Launch", 8));

            var card = CreateCard(page);
            _togStartupPage = card.AddRow(
                "Run at Windows startup",
                "Start SmartTaskbar in the tray when you sign in. The settings window stays closed.");
            _togStartupPage.CheckedChanged += (_, _) => OnStartupToggled(_togStartupPage.Checked);

            var info = new InfoBar(
                "Stored in the current-user Run key (HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run). No administrator rights are needed.");
            page.Controls.Add(info);
            RegisterPageLayout(page, card, info);
            return page;
        }

        private Panel CreatePage() => new Panel
        {
            BackColor = Win11Theme.Background,
            AutoScroll = true,
            Visible = false
        };

        private SettingsCard CreateCard(Panel page)
        {
            var card = new SettingsCard { Location = new Point(Inset, CardTop) };
            page.Controls.Add(card);
            return card;
        }

        private static Label CreateSectionLabel(string text, int y) => new Label
        {
            Text = text,
            Font = Win11Theme.SectionFont,
            ForeColor = Win11Theme.TextPrimary,
            BackColor = Win11Theme.Background,
            AutoSize = true,
            Location = new Point(Inset, y)
        };

        /// <summary>
        ///     Registers how a page's card and info bar stretch with the window.
        /// </summary>
        private void RegisterPageLayout(Panel page, SettingsCard card, InfoBar? info)
        {
            _pageLayouts.Add(() =>
            {
                int width = Math.Max(200, page.ClientSize.Width - Inset * 2);
                card.Location = new Point(Inset, CardTop);
                card.Width = width;

                if (info != null)
                {
                    info.Fit(width);
                    info.Location = new Point(Inset, card.Bottom + CardGap);
                }
            });
        }

        private void ShowPage(int index)
        {
            for (int i = 0; i < _pages.Length; i++)
                _pages[i].Visible = i == index;
        }

        // ===== Layout =====

        private void LayoutContent()
        {
            if (!_layoutReady) return;

            _tabs.SetBounds(0, TabTop, ClientSize.Width, TabHeight);

            int pageHeight = Math.Max(1, ClientSize.Height - PageTop);
            foreach (var page in _pages)
                page.SetBounds(0, PageTop, ClientSize.Width, pageHeight);

            foreach (var layout in _pageLayouts)
                layout();

            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutContent();
        }

        // ===== Borderless window: frame, resize, drag and maximize =====

        protected override CreateParams CreateParams
        {
            get
            {
                // Keep the resizable frame (for edge resizing and Aero Snap) and add a drop shadow.
                // The non-client area is removed in WM_NCCALCSIZE, so no default title bar is drawn.
                CreateParams cp = base.CreateParams;
                cp.Style |= WsThickFrame | WsMinimizeBox | WsMaximizeBox;
                cp.ClassStyle |= CsDropShadow;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WmNcCalcSize when m.WParam != IntPtr.Zero:
                    // Client area = whole window. When maximized, fit inside the work area (no overhang).
                    ConstrainToWorkArea(m.LParam);
                    m.Result = IntPtr.Zero;
                    return;

                case WmNcHitTest:
                    m.Result = (IntPtr)HitTestNonClient(PointToClient(LParamToPoint(m.LParam)));
                    return;

                case WmNcMouseMove:
                    ClearCaptionHover();
                    break;

                case WmNcLButtonDblClk when m.WParam.ToInt32() == HtCaption:
                    ToggleMaximize();
                    return;
            }

            base.WndProc(ref m);
        }

        private void ConstrainToWorkArea(IntPtr lParam)
        {
            if (WindowState != FormWindowState.Maximized) return;

            var rect = Marshal.PtrToStructure<NativeRect>(lParam);
            Rectangle work = Screen.FromHandle(Handle).WorkingArea;
            rect.Left = work.Left;
            rect.Top = work.Top;
            rect.Right = work.Right;
            rect.Bottom = work.Bottom;
            Marshal.StructureToPtr(rect, lParam, false);
        }

        private int HitTestNonClient(Point p)
        {
            if (WindowState != FormWindowState.Maximized)
            {
                bool left = p.X < ResizeBorder;
                bool right = p.X >= ClientSize.Width - ResizeBorder;
                bool top = p.Y < ResizeBorder;
                bool bottom = p.Y >= ClientSize.Height - ResizeBorder;

                if (top && left) return HtTopLeft;
                if (top && right) return HtTopRight;
                if (bottom && left) return HtBottomLeft;
                if (bottom && right) return HtBottomRight;
                if (left) return HtLeft;
                if (right) return HtRight;
                if (top) return HtTop;
                if (bottom) return HtBottom;
            }

            // Empty title bar area drags the window; caption buttons stay clickable as client area.
            if (p.Y < CaptionHeight && CaptionButtonAt(p) == CaptionButton.None)
                return HtCaption;

            return HtClient;
        }

        private static Point LParamToPoint(IntPtr lParam)
        {
            long value = lParam.ToInt64();
            return new Point(unchecked((short)(value & 0xFFFF)),
                             unchecked((short)((value >> 16) & 0xFFFF)));
        }

        private void ToggleMaximize()
            => WindowState = WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;

        // ===== Caption bar (title + min / max / close) =====

        private Rectangle CaptionButtonBounds(CaptionButton button)
        {
            int slot = button switch
            {
                CaptionButton.Maximize => 1,
                CaptionButton.Minimize => 2,
                _ => 0
            };

            return new Rectangle(ClientSize.Width - CaptionButtonWidth * (slot + 1), 0,
                                 CaptionButtonWidth, CaptionHeight);
        }

        private CaptionButton CaptionButtonAt(Point p)
        {
            if (p.Y < 0 || p.Y >= CaptionHeight) return CaptionButton.None;

            foreach (CaptionButton button in new[] { CaptionButton.Minimize, CaptionButton.Maximize, CaptionButton.Close })
            {
                if (CaptionButtonBounds(button).Contains(p))
                    return button;
            }
            return CaptionButton.None;
        }

        private void InvalidateCaption()
            => Invalidate(new Rectangle(0, 0, ClientSize.Width, CaptionHeight));

        private void ClearCaptionHover()
        {
            if (_hoverButton == CaptionButton.None && _pressedButton == CaptionButton.None) return;

            _hoverButton = CaptionButton.None;
            _pressedButton = CaptionButton.None;
            InvalidateCaption();
        }

        private void RunCaptionButton(CaptionButton button)
        {
            switch (button)
            {
                case CaptionButton.Minimize:
                    WindowState = FormWindowState.Minimized;
                    break;
                case CaptionButton.Maximize:
                    ToggleMaximize();
                    break;
                case CaptionButton.Close:
                    Close(); // FormClosing hides the window; the tray keeps running.
                    break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            CaptionButton hover = CaptionButtonAt(e.Location);
            if (hover != _hoverButton)
            {
                _hoverButton = hover;
                InvalidateCaption();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            ClearCaptionHover();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressedButton = CaptionButtonAt(e.Location);
                InvalidateCaption();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _pressedButton != CaptionButton.None)
            {
                CaptionButton pressed = _pressedButton;
                CaptionButton released = CaptionButtonAt(e.Location);

                _pressedButton = CaptionButton.None;
                InvalidateCaption();

                if (released == pressed)
                    RunCaptionButton(pressed);
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            var icon = Icon;
            if (icon is not null)
                g.DrawIcon(icon, new Rectangle(12, (CaptionHeight - 16) / 2, 16, 16));

            TextRenderer.DrawText(g, "SmartTaskbar", Win11Theme.CaptionFont,
                                  new Rectangle(36, 0, 240, CaptionHeight),
                                  Win11Theme.TextSecondary,
                                  TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                                  | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            DrawCaptionButton(g, CaptionButton.Minimize, "\uE921");
            DrawCaptionButton(g, CaptionButton.Maximize,
                              WindowState == FormWindowState.Maximized ? "\uE923" : "\uE922");
            DrawCaptionButton(g, CaptionButton.Close, "\uE8BB");
        }

        private void DrawCaptionButton(Graphics g, CaptionButton button, string glyph)
        {
            Rectangle rect = CaptionButtonBounds(button);
            bool hovered = _hoverButton == button;
            bool pressed = hovered && _pressedButton == button;

            Color? fill = null;
            if (button == CaptionButton.Close)
            {
                if (pressed) fill = Color.FromArgb(0xA0, 0x2A, 0x1E);
                else if (hovered) fill = Color.FromArgb(0xC4, 0x2B, 0x1C);
            }
            else if (hovered)
            {
                fill = pressed ? Color.FromArgb(50, 50, 50) : Color.FromArgb(45, 45, 45);
            }

            if (fill.HasValue)
            {
                using var brush = new SolidBrush(fill.Value);
                g.FillRectangle(brush, rect);
            }

            TextRenderer.DrawText(g, glyph, Win11Theme.IconFont, rect, Win11Theme.TextPrimary,
                                  TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                                  | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        // ===== Change handlers =====

        private void OnEnabledToggled(bool enabled)
        {
            if (_suppress) return;

            _actions.SetEnabled(enabled);
            SyncFromSettings();
        }

        private void OnAutoModeToggled(bool enabled)
        {
            if (_suppress) return;

            _actions.SetAutoMode(enabled);
            SyncFromSettings();
        }

        private void OnStartupToggled(bool enabled)
        {
            if (_suppress) return;

            StartupHelper.SetRunAtStartup(enabled);
            SyncFromSettings();   // re-reads the registry, so a failed write reverts the switch
        }

        private void OnCenterToggled(bool enabled)
        {
            if (_suppress) return;

            _actions.SetCenter(enabled);
            SyncFromSettings();
        }

        private void OnAnimationToggled(bool enabled)
        {
            if (_suppress) return;

            if (enabled != Fun.IsEnableTaskbarAnimation())
                Fun.ChangeTaskbarAnimation();

            SyncFromSettings();
        }

        private void OnEasingChanged(int index)
        {
            if (_suppress || index < 0 || index >= CenterOptions.Easings.Length) return;

            UserSettings.CenterEasing = CenterOptions.Easings[index];
            _actions.ApplyCenterOptions();
        }

        private void OnSpeedChanged(int index)
        {
            if (_suppress || index < 0 || index >= CenterOptions.SpeedMs.Length) return;

            UserSettings.CenterDurationMs = CenterOptions.SpeedMs[index];
            _actions.ApplyCenterOptions();
        }

        private void OnShowOnExitToggled(bool enabled)
        {
            if (_suppress) return;

            UserSettings.ShowTaskbarWhenExit = enabled;
        }

        // ===== State sync =====

        /// <summary>
        ///     Reads every setting (UserSettings, registry, system animation) into the controls
        ///     without triggering writes.
        /// </summary>
        private void SyncFromSettings()
        {
            try
            {
                bool enabled = UserSettings.Enabled;
                bool autoOn = UserSettings.AutoModeType == AutoModeType.Auto;
                bool startupOn = StartupHelper.IsRunAtStartup();
                bool centered = TaskbarAlignmentHelper.IsSupported
                    ? TaskbarAlignmentHelper.IsCentered()
                    : UserSettings.CenterIcons;
                bool animationOn = Fun.IsEnableTaskbarAnimation();
                bool showOnExit = UserSettings.ShowTaskbarWhenExit;

                int easingIndex = Array.IndexOf(CenterOptions.Easings, UserSettings.CenterEasing);
                int speedIndex = Array.IndexOf(CenterOptions.SpeedMs, UserSettings.CenterDurationMs);

                WithSuppressed(() =>
                {
                    _togEnabled.Checked = enabled;
                    _togAutoMain.Checked = autoOn;
                    _togAutoStyle.Checked = autoOn;
                    _togStartupMain.Checked = startupOn;
                    _togStartupPage.Checked = startupOn;
                    _togCenter.Checked = centered;
                    _togAnimation.Checked = animationOn;
                    _togShowOnExit.Checked = showOnExit;

                    if (easingIndex >= 0) _easingCombo.SelectedIndex = easingIndex;
                    if (speedIndex >= 0) _speedCombo.SelectedIndex = speedIndex;
                });

                UpdateStatus(enabled, autoOn);
            }
            catch
            {
                // Keep the window usable even if a system query fails.
            }
        }

        private void UpdateStatus(bool enabled, bool autoOn)
        {
            if (!enabled)
            {
                _lblStatus.Text = "SmartTaskbar is off";
                _lblStatus.ForeColor = Win11Theme.TextSecondary;
            }
            else if (autoOn)
            {
                _lblStatus.Text = "Auto Mode is on";
                _lblStatus.ForeColor = Win11Theme.Accent;
            }
            else
            {
                _lblStatus.Text = "Auto Mode is off";
                _lblStatus.ForeColor = Win11Theme.TextSecondary;
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
        public void RefreshState() => SyncFromSettings();

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
            SyncFromSettings();
            BringToFront();
            Activate();
        }

        /// <summary>
        ///     Dark mode for system parts (scrollbars, menus) and rounded corners on Windows 11.
        ///     Each attribute is optional, so older builds ignore it.
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
            }
            catch
            {
                // Non-fatal: the window works without these attributes.
            }
        }
    }
}
