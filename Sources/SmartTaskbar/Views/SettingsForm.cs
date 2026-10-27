using System.Drawing.Drawing2D;
using Microsoft.Win32;
using SmartTaskbar.Languages;

namespace SmartTaskbar
{
    internal sealed class SettingsForm : Form
    {
        private readonly Panel _contentPanel;
        private readonly Button[] _tabButtons;
        private readonly Panel[] _pages;
        private int _activeTab;

        // Animation page controls
        private CheckBox _chkTaskbarAnimation = null!;

        // Style page controls
        private CheckBox _chkAutoMode = null!;
        private CheckBox _chkShowBarOnExit = null!;

        // Startup page controls
        private CheckBox _chkRunAtStartup = null!;

        private static readonly Color BgDark = Color.FromArgb(32, 32, 32);
        private static readonly Color CardBg = Color.FromArgb(45, 45, 45);
        private static readonly Color Accent = Color.FromArgb(0, 120, 212);
        private static readonly Color TextPrimary = Color.FromArgb(240, 240, 240);
        private static readonly Color TextSecondary = Color.FromArgb(180, 180, 180);
        private static readonly Color SidebarBg = Color.FromArgb(25, 25, 25);

        public SettingsForm()
        {
            Text = "SmartTaskbar Settings";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(720, 480);
            BackColor = BgDark;
            Font = new Font("Segoe UI", 9.75F);
            ShowInTaskbar = true;
            Icon = Fun.IsLightTheme() ? IconResource.Logo_Black : IconResource.Logo_White;

            // Sidebar
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 180,
                BackColor = SidebarBg
            };

            var titleLabel = new Label
            {
                Text = "SmartTaskbar",
                Font = new Font("Segoe UI Semibold", 12F),
                ForeColor = TextPrimary,
                Location = new Point(16, 20),
                AutoSize = true
            };
            sidebar.Controls.Add(titleLabel);

            _tabButtons = new Button[3];
            string[] tabNames = { "Animation", "Style", "Startup" };
            for (int i = 0; i < 3; i++)
            {
                var btn = new Button
                {
                    Text = "  " + tabNames[i],
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(50, 50, 50) },
                    BackColor = i == 0 ? CardBg : Color.Transparent,
                    ForeColor = TextPrimary,
                    Font = new Font("Segoe UI", 10F),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Size = new Size(164, 40),
                    Location = new Point(8, 70 + i * 48),
                    Cursor = Cursors.Hand,
                    Tag = i
                };
                btn.Click += TabButton_Click;
                _tabButtons[i] = btn;
                sidebar.Controls.Add(btn);
            }

            // Content area
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(24)
            };

            _pages = new Panel[3];
            _pages[0] = BuildAnimationPage();
            _pages[1] = BuildStylePage();
            _pages[2] = BuildStartupPage();

            foreach (var page in _pages)
            {
                page.Dock = DockStyle.Fill;
                page.Visible = false;
                _contentPanel.Controls.Add(page);
            }
            _pages[0].Visible = true;

            Controls.Add(_contentPanel);
            Controls.Add(sidebar);

            LoadSettings();
            FormClosing += SettingsForm_FormClosing;
        }

        private Panel BuildAnimationPage()
        {
            var page = new Panel { BackColor = BgDark };

            var card = CreateCard(0, 0, 480, 160);
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
                Size = new Size(430, 36)
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
                // Only apply when user changes it
                if (_chkTaskbarAnimation.Checked != Fun.IsEnableTaskbarAnimation())
                    Fun.ChangeTaskbarAnimation();
            };
            card.Controls.Add(_chkTaskbarAnimation);

            return page;
        }

        private Panel BuildStylePage()
        {
            var page = new Panel { BackColor = BgDark };

            var card1 = CreateCard(0, 0, 480, 140);
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
                Size = new Size(430, 36)
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
            };
            card1.Controls.Add(_chkAutoMode);

            var card2 = CreateCard(0, 160, 480, 140);
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
                Size = new Size(430, 36)
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

            var card = CreateCard(0, 0, 480, 160);
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
                Text = "Start SmartTaskbar automatically when you sign in to Windows.",
                ForeColor = TextSecondary,
                Location = new Point(20, 48),
                Size = new Size(430, 36)
            };
            card.Controls.Add(desc);

            _chkRunAtStartup = new CheckBox
            {
                Text = "Run at Windows startup",
                ForeColor = TextPrimary,
                Location = new Point(20, 100),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            _chkRunAtStartup.CheckedChanged += (s, e) =>
            {
                StartupHelper.SetRunAtStartup(_chkRunAtStartup.Checked);
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
            // Rounded look via region (simple)
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
            _chkTaskbarAnimation.Checked = Fun.IsEnableTaskbarAnimation();
            _chkAutoMode.Checked = UserSettings.AutoModeType == AutoModeType.Auto;
            _chkShowBarOnExit.Checked = UserSettings.ShowTaskbarWhenExit;
            _chkRunAtStartup.Checked = StartupHelper.IsRunAtStartup();
        }

        private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Keep the app running in tray; just hide the form
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Refresh values every time the window is shown
            LoadSettings();
        }
    }
}
