using System.Drawing.Drawing2D;

namespace SmartTaskbar
{
    internal sealed class SettingsForm : Form
    {
        private readonly Panel _tabBar;
        private readonly Button[] _tabButtons;
        private readonly Panel[] _pages;
        private int _activeTab;

        private Label _lblStatus = null!;
        private CheckBox _chkMainAutoMode = null!;
        private CheckBox _chkMainStartup = null!;
        private CheckBox _chkTaskbarAnimation = null!;
        private CheckBox _chkAutoMode = null!;
        private CheckBox _chkShowBarOnExit = null!;
        private CheckBox _chkRunAtStartup = null!;

        // Windows 11 dark palette
        private static readonly Color BgDark = Color.FromArgb(32, 32, 32);       // #202020
        private static readonly Color CardBg = Color.FromArgb(43, 43, 43);       // #2B2B2B
        private static readonly Color TabBarBg = Color.FromArgb(28, 28, 28);
        private static readonly Color Accent = Color.FromArgb(96, 205, 255);     // Win11 accent-ish
        private static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
        private static readonly Color TextSecondary = Color.FromArgb(200, 200, 200);
        private static readonly Color Border = Color.FromArgb(60, 60, 60);

        private static readonly string[] TabNames = { "Main", "Animation", "Style", "Startup" };

        public SettingsForm()
        {
            Text = "SmartTaskbar";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(700, 480);
            BackColor = BgDark;
            Font = new Font("Segoe UI", 10F);
            ShowInTaskbar = true;
            DoubleBuffered = true;

            try
            {
                Icon = Fun.IsLightTheme() ? IconResource.Logo_Black : IconResource.Logo_White;
            }
            catch { /* optional */ }

            // Top navigation bar
            _tabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = TabBarBg
            };
            _tabBar.Paint += (s, e) =>
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, 0, _tabBar.Height - 1, _tabBar.Width, _tabBar.Height - 1);
            };

            _tabButtons = new Button[TabNames.Length];
            int tabW = 120;
            int gap = 4;
            int startX = 16;

            for (int i = 0; i < TabNames.Length; i++)
            {
                var btn = new Button
                {
                    Text = TabNames[i],
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance =
                    {
                        BorderSize = 0,
                        MouseOverBackColor = Color.FromArgb(50, 50, 50),
                        MouseDownBackColor = Color.FromArgb(55, 55, 55)
                    },
                    BackColor = Color.Transparent,
                    ForeColor = i == 0 ? TextPrimary : TextSecondary,
                    Font = new Font("Segoe UI Semibold", 10F),
                    Size = new Size(tabW, 40),
                    Location = new Point(startX + i * (tabW + gap), 8),
                    Cursor = Cursors.Hand,
                    Tag = i,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                int idx = i;
                btn.Paint += (s, e) =>
                {
                    if (idx == _activeTab)
                    {
                        using var pen = new Pen(Accent, 2.5f);
                        int underlineY = btn.Height - 4;
                        int pad = 18;
                        e.Graphics.DrawLine(pen, pad, underlineY, btn.Width - pad, underlineY);
                    }
                };

                btn.Click += TabButton_Click;
                _tabButtons[i] = btn;
                _tabBar.Controls.Add(btn);
            }

            _pages = new Panel[TabNames.Length];
            _pages[0] = BuildMainPage();
            _pages[1] = BuildAnimationPage();
            _pages[2] = BuildStylePage();
            _pages[3] = BuildStartupPage();

            foreach (var page in _pages)
            {
                page.Dock = DockStyle.Fill;
                page.Visible = false;
                page.Padding = new Padding(28, 24, 28, 24);
                Controls.Add(page);
            }
            _pages[0].Visible = true;

            Controls.Add(_tabBar);

            LoadSettings();
            FormClosing += SettingsForm_FormClosing;
        }

        private Panel BuildMainPage()
        {
            var page = new Panel { BackColor = BgDark };

            var card = CreateCard(0, 0, 640, 220);
            page.Controls.Add(card);

            AddLabel(card, "SmartTaskbar", 20, 18, true, 15F);
            _lblStatus = AddLabel(card, "Status: Running", 20, 52, false, 10F, TextSecondary);

            _chkMainAutoMode = AddCheck(card, "Enable Auto Mode", 20, 100);
            _chkMainAutoMode.CheckedChanged += (s, e) =>
            {
                UserSettings.AutoModeType = _chkMainAutoMode.Checked ? AutoModeType.Auto : AutoModeType.None;
                if (_chkAutoMode != null) _chkAutoMode.Checked = _chkMainAutoMode.Checked;
                if (_lblStatus != null)
                    _lblStatus.Text = _chkMainAutoMode.Checked ? "Status: Auto Mode ON" : "Status: Auto Mode OFF";
            };

            _chkMainStartup = AddCheck(card, "Run at Windows startup", 20, 140);
            _chkMainStartup.CheckedChanged += (s, e) =>
            {
                StartupHelper.SetRunAtStartup(_chkMainStartup.Checked);
                if (_chkRunAtStartup != null) _chkRunAtStartup.Checked = _chkMainStartup.Checked;
            };

            AddLabel(card, "Closing this window keeps SmartTaskbar running in the system tray.", 20, 185, false, 9F, TextSecondary);

            return page;
        }

        private Panel BuildAnimationPage()
        {
            var page = new Panel { BackColor = BgDark };
            var card = CreateCard(0, 0, 640, 170);
            page.Controls.Add(card);

            AddLabel(card, "Taskbar Animation", 20, 18, true, 12F);
            AddLabel(card, "Enable or disable the system taskbar animation effect.", 20, 52, false, 9.5F, TextSecondary);

            _chkTaskbarAnimation = AddCheck(card, "Enable taskbar animation", 20, 110);
            _chkTaskbarAnimation.CheckedChanged += (s, e) =>
            {
                if (_chkTaskbarAnimation.Checked != Fun.IsEnableTaskbarAnimation())
                    Fun.ChangeTaskbarAnimation();
            };

            return page;
        }

        private Panel BuildStylePage()
        {
            var page = new Panel { BackColor = BgDark };

            var card1 = CreateCard(0, 0, 640, 150);
            page.Controls.Add(card1);
            AddLabel(card1, "Auto Mode", 20, 16, true, 12F);
            AddLabel(card1, "Hide the taskbar automatically when the focused window intersects it.", 20, 48, false, 9.5F, TextSecondary);
            _chkAutoMode = AddCheck(card1, "Enable Auto Mode", 20, 100);
            _chkAutoMode.CheckedChanged += (s, e) =>
            {
                UserSettings.AutoModeType = _chkAutoMode.Checked ? AutoModeType.Auto : AutoModeType.None;
                if (_chkMainAutoMode != null) _chkMainAutoMode.Checked = _chkAutoMode.Checked;
            };

            var card2 = CreateCard(0, 170, 640, 150);
            page.Controls.Add(card2);
            AddLabel(card2, "Exit Behavior", 20, 16, true, 12F);
            AddLabel(card2, "Show the taskbar again when SmartTaskbar exits.", 20, 48, false, 9.5F, TextSecondary);
            _chkShowBarOnExit = AddCheck(card2, "Show taskbar on exit", 20, 100);
            _chkShowBarOnExit.CheckedChanged += (s, e) =>
            {
                UserSettings.ShowTaskbarWhenExit = _chkShowBarOnExit.Checked;
            };

            return page;
        }

        private Panel BuildStartupPage()
        {
            var page = new Panel { BackColor = BgDark };
            var card = CreateCard(0, 0, 640, 190);
            page.Controls.Add(card);

            AddLabel(card, "Windows Startup", 20, 18, true, 12F);
            AddLabel(card, "Start SmartTaskbar automatically when you sign in to Windows.\nUses the current-user Run key (no administrator rights needed).", 20, 52, false, 9.5F, TextSecondary);

            _chkRunAtStartup = AddCheck(card, "Run at Windows startup", 20, 125);
            _chkRunAtStartup.CheckedChanged += (s, e) =>
            {
                StartupHelper.SetRunAtStartup(_chkRunAtStartup.Checked);
                if (_chkMainStartup != null) _chkMainStartup.Checked = _chkRunAtStartup.Checked;
            };

            return page;
        }

        private static Label AddLabel(Control parent, string text, int x, int y, bool bold, float size, Color? color = null)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color ?? TextPrimary,
                Location = new Point(x, y),
                AutoSize = true
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private static CheckBox AddCheck(Control parent, string text, int x, int y)
        {
            var chk = new CheckBox
            {
                Text = text,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 10.5F),
                Location = new Point(x, y),
                AutoSize = true,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System
            };
            parent.Controls.Add(chk);
            return chk;
        }

        private static Panel CreateCard(int x, int y, int w, int h)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = CardBg
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = RoundedRect(rect, 10);
                using var brush = new SolidBrush(CardBg);
                e.Graphics.FillPath(brush, path);
                using var pen = new Pen(Border);
                e.Graphics.DrawPath(pen, path);
            };

            return card;
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void TabButton_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int index) return;
            if (index == _activeTab) return;

            _tabButtons[_activeTab].ForeColor = TextSecondary;
            _tabButtons[_activeTab].Invalidate();
            _pages[_activeTab].Visible = false;

            _activeTab = index;
            _tabButtons[_activeTab].ForeColor = TextPrimary;
            _tabButtons[_activeTab].Invalidate();
            _pages[_activeTab].Visible = true;
        }

        private void LoadSettings()
        {
            try
            {
                bool auto = UserSettings.AutoModeType == AutoModeType.Auto;
                bool showExit = UserSettings.ShowTaskbarWhenExit;
                bool startup = StartupHelper.IsRunAtStartup();
                bool anim = Fun.IsEnableTaskbarAnimation();

                if (_chkMainAutoMode != null) _chkMainAutoMode.Checked = auto;
                if (_chkMainStartup != null) _chkMainStartup.Checked = startup;
                if (_chkTaskbarAnimation != null) _chkTaskbarAnimation.Checked = anim;
                if (_chkAutoMode != null) _chkAutoMode.Checked = auto;
                if (_chkShowBarOnExit != null) _chkShowBarOnExit.Checked = showExit;
                if (_chkRunAtStartup != null) _chkRunAtStartup.Checked = startup;

                if (_lblStatus != null)
                    _lblStatus.Text = auto ? "Status: Auto Mode ON" : "Status: Auto Mode OFF";
            }
            catch
            {
                // keep UI alive
            }
        }

        private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadSettings();
            BringToFront();
            Activate();
        }
    }
}
