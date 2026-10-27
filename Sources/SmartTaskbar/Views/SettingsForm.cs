using System.Drawing.Drawing2D;

namespace SmartTaskbar
{
    internal sealed class SettingsForm : Form
    {
        private readonly Panel _tabBar;
        private readonly Button[] _tabButtons;
        private readonly Panel[] _pages;
        private int _activeTab;

        // Main
        private Label _lblStatus = null!;
        private CheckBox _chkMainAutoMode = null!;
        private CheckBox _chkMainStartup = null!;

        // Animation
        private CheckBox _chkTaskbarAnimation = null!;

        // Style
        private CheckBox _chkAutoMode = null!;
        private CheckBox _chkShowBarOnExit = null!;

        // Startup
        private CheckBox _chkRunAtStartup = null!;

        private static readonly Color BgDark = Color.FromArgb(32, 32, 32);
        private static readonly Color CardBg = Color.FromArgb(45, 45, 45);
        private static readonly Color TabBarBg = Color.FromArgb(25, 25, 25);
        private static readonly Color Accent = Color.FromArgb(0, 120, 212);
        private static readonly Color TextPrimary = Color.FromArgb(240, 240, 240);
        private static readonly Color TextSecondary = Color.FromArgb(180, 180, 180);

        private static readonly string[] TabNames = { "Main", "Animation", "Style", "Startup" };

        public SettingsForm()
        {
            Text = "SmartTaskbar";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(680, 460);
            BackColor = BgDark;
            Font = new Font("Segoe UI", 9.75F);
            ShowInTaskbar = true;
            try
            {
                Icon = Fun.IsLightTheme() ? IconResource.Logo_Black : IconResource.Logo_White;
            }
            catch
            {
                // Icon optional
            }

            // ===== Top tab bar =====
            _tabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = TabBarBg
            };

            _tabButtons = new Button[TabNames.Length];
            int tabWidth = 110;
            int startX = 12;
            for (int i = 0; i < TabNames.Length; i++)
            {
                var btn = new Button
                {
                    Text = TabNames[i],
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(50, 50, 50) },
                    BackColor = i == 0 ? CardBg : Color.Transparent,
                    ForeColor = TextPrimary,
                    Font = new Font("Segoe UI Semibold", 10F),
                    Size = new Size(tabWidth, 36),
                    Location = new Point(startX + i * (tabWidth + 4), 6),
                    Cursor = Cursors.Hand,
                    Tag = i
                };
                btn.Click += TabButton_Click;
                _tabButtons[i] = btn;
                _tabBar.Controls.Add(btn);
            }

            // ===== Content pages =====
            _pages = new Panel[TabNames.Length];
            _pages[0] = BuildMainPage();
            _pages[1] = BuildAnimationPage();
            _pages[2] = BuildStylePage();
            _pages[3] = BuildStartupPage();

            foreach (var page in _pages)
            {
                page.Dock = DockStyle.Fill;
                page.Visible = false;
                page.Padding = new Padding(24);
                Controls.Add(page);
            }
            _pages[0].Visible = true;

            Controls.Add(_tabBar); // add last so it docks on top

            LoadSettings();
            FormClosing += SettingsForm_FormClosing;
        }

        private Panel BuildMainPage()
        {
            var page = new Panel { BackColor = BgDark };

            var card = CreateCard(0, 0, 620, 200);
            page.Controls.Add(card);

            var header = new Label
            {
                Text = "SmartTaskbar",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = TextPrimary,
                Location = new Point(20, 16),
                AutoSize = true
            };
            card.Controls.Add(header);

            _lblStatus = new Label
            {
                Text = "Status: Running",
                ForeColor = TextSecondary,
                Location = new Point(20, 48),
                AutoSize = true
            };
            card.Controls.Add(_lblStatus);

            _chkMainAutoMode = new CheckBox
            {
                Text = "Enable Auto Mode",
                ForeColor = TextPrimary,
                Location = new Point(20, 90),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            _chkMainAutoMode.CheckedChanged += (s, e) =>
            {
                UserSettings.AutoModeType = _chkMainAutoMode.Checked ? AutoModeType.Auto : AutoModeType.None;
                if (_chkAutoMode != null)
                    _chkAutoMode.Checked = _chkMainAutoMode.Checked;
            };
            card.Controls.Add(_chkMainAutoMode);

            _chkMainStartup = new CheckBox
            {
                Text = "Run at Windows startup",
                ForeColor = TextPrimary,
                Location = new Point(20, 125),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            _chkMainStartup.CheckedChanged += (s, e) =>
            {
                StartupHelper.SetRunAtStartup(_chkMainStartup.Checked);
                if (_chkRunAtStartup != null)
                    _chkRunAtStartup.Checked = _chkMainStartup.Checked;
            };
            card.Controls.Add(_chkMainStartup);

            var tip = new Label
            {
                Text = "Use the tabs above for more options. Closing this window keeps SmartTaskbar in the tray.",
                ForeColor = TextSecondary,
                Location = new Point(20, 165),
                Size = new Size(560, 24)
            };
            card.Controls.Add(tip);

            return page;
        }

        private Panel BuildAnimationPage()
        {
            var page = new Panel { BackColor = BgDark };

            var card = CreateCard(0, 0, 620, 160);
            page.Controls.Add(card);

            var header = new Label
            {
                Text = "Taskbar Animation",
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = TextPrimary,
                Location = new Point(20, 18),
                AutoSize = true
            };
            card.Controls.Add(header);

            var desc = new Label
            {
                Text = "Enable or disable the system taskbar animation effect.",
                ForeColor = TextSecondary,
                Location = new Point(20, 48),
                Size = new Size(560, 36)
            };
            card.Controls.Add(desc);

            _chkTaskbarAnimation = new CheckBox
            {
                Text = "Enable taskbar animation",
                ForeColor = TextPrimary,
                Location = new Point(20, 100),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            _chkTaskbarAnimation.CheckedChanged += (s, e) =>
            {
                if (_chkTaskbarAnimation.Checked != Fun.IsEnableTaskbarAnimation())
                    Fun.ChangeTaskbarAnimation();
            };
            card.Controls.Add(_chkTaskbarAnimation);

            return page;
        }

        private Panel BuildStylePage()
        {
            var page = new Panel { BackColor = BgDark };

            var card1 = CreateCard(0, 0, 620, 140);
            page.Controls.Add(card1);

            var h1 = new Label
            {
                Text = "Auto Mode",
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = TextPrimary,
                Location = new Point(20, 18),
                AutoSize = true
            };
            card1.Controls.Add(h1);

            var d1 = new Label
            {
                Text = "Automatically hide the taskbar when the focused window intersects it.",
                ForeColor = TextSecondary,
                Location = new Point(20, 48),
                Size = new Size(560, 36)
            };
            card1.Controls.Add(d1);

            _chkAutoMode = new CheckBox
            {
                Text = "Enable Auto Mode",
                ForeColor = TextPrimary,
                Location = new Point(20, 95),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            _chkAutoMode.CheckedChanged += (s, e) =>
            {
                UserSettings.AutoModeType = _chkAutoMode.Checked ? AutoModeType.Auto : AutoModeType.None;
                if (_chkMainAutoMode != null)
                    _chkMainAutoMode.Checked = _chkAutoMode.Checked;
            };
            card1.Controls.Add(_chkAutoMode);

            var card2 = CreateCard(0, 160, 620, 140);
            page.Controls.Add(card2);

            var h2 = new Label
            {
                Text = "Exit Behavior",
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = TextPrimary,
                Location = new Point(20, 18),
                AutoSize = true
            };
            card2.Controls.Add(h2);

            var d2 = new Label
            {
                Text = "Show the taskbar again when SmartTaskbar exits.",
                ForeColor = TextSecondary,
                Location = new Point(20, 48),
                Size = new Size(560, 36)
            };
            card2.Controls.Add(d2);

            _chkShowBarOnExit = new CheckBox
            {
                Text = "Show taskbar on exit",
                ForeColor = TextPrimary,
                Location = new Point(20, 95),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            _chkShowBarOnExit.CheckedChanged += (s, e) =>
            {
                UserSettings.ShowTaskbarWhenExit = _chkShowBarOnExit.Checked;
            };
            card2.Controls.Add(_chkShowBarOnExit);

            return page;
        }

        private Panel BuildStartupPage()
        {
            var page = new Panel { BackColor = BgDark };

            var card = CreateCard(0, 0, 620, 180);
            page.Controls.Add(card);

            var header = new Label
            {
                Text = "Windows Startup",
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = TextPrimary,
                Location = new Point(20, 18),
                AutoSize = true
            };
            card.Controls.Add(header);

            var desc = new Label
            {
                Text = "Start SmartTaskbar automatically when you sign in to Windows.\nThis writes to the current-user Run registry key (no admin required).",
                ForeColor = TextSecondary,
                Location = new Point(20, 48),
                Size = new Size(560, 50)
            };
            card.Controls.Add(desc);

            _chkRunAtStartup = new CheckBox
            {
                Text = "Run at Windows startup",
                ForeColor = TextPrimary,
                Location = new Point(20, 115),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            _chkRunAtStartup.CheckedChanged += (s, e) =>
            {
                StartupHelper.SetRunAtStartup(_chkRunAtStartup.Checked);
                if (_chkMainStartup != null)
                    _chkMainStartup.Checked = _chkRunAtStartup.Checked;
            };
            card.Controls.Add(_chkRunAtStartup);

            return page;
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
                using var path = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
                using var brush = new SolidBrush(CardBg);
                e.Graphics.FillPath(brush, path);
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

            _tabButtons[_activeTab].BackColor = Color.Transparent;
            _pages[_activeTab].Visible = false;

            _activeTab = index;
            _tabButtons[_activeTab].BackColor = CardBg;
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
                // Keep UI alive even if settings fail
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
