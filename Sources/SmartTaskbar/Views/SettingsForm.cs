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
        private ToggleSwitch _togMainAuto = null!;
        private ToggleSwitch _togMainStartup = null!;
        private ToggleSwitch _togAnimation = null!;
        private ToggleSwitch _togAutoMode = null!;
        private ToggleSwitch _togShowOnExit = null!;
        private ToggleSwitch _togStartup = null!;

        // Win11 dark palette (matches Settings app)
        private static readonly Color BgDark = Color.FromArgb(32, 32, 32);
        private static readonly Color CardBg = Color.FromArgb(43, 43, 43);
        private static readonly Color TabBarBg = Color.FromArgb(32, 32, 32);
        private static readonly Color Accent = Color.FromArgb(96, 205, 255);
        private static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
        private static readonly Color TextSecondary = Color.FromArgb(190, 190, 190);
        private static readonly Color Border = Color.FromArgb(55, 55, 55);
        private static readonly Color Divider = Color.FromArgb(55, 55, 55);

        private static readonly string[] TabNames = { "Main", "Animation", "Style", "Startup" };

        public SettingsForm()
        {
            Text = "SmartTaskbar";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(720, 520);
            BackColor = BgDark;
            Font = new Font("Segoe UI", 10F);
            ShowInTaskbar = true;
            DoubleBuffered = true;

            try
            {
                Icon = Fun.IsLightTheme() ? IconResource.Logo_Black : IconResource.Logo_White;
            }
            catch { }

            _tabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = TabBarBg
            };
            _tabBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, 0, _tabBar.Height - 1, _tabBar.Width, _tabBar.Height - 1);
            };

            _tabButtons = new Button[TabNames.Length];
            const int tabW = 110;
            const int gap = 2;
            const int startX = 20;

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
                    Size = new Size(tabW, 36),
                    Location = new Point(startX + i * (tabW + gap), 8),
                    Cursor = Cursors.Hand,
                    Tag = i,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                int idx = i;
                btn.Paint += (_, e) =>
                {
                    if (idx != _activeTab) return;
                    using var pen = new Pen(Accent, 2.5f);
                    int y = btn.Height - 3;
                    e.Graphics.DrawLine(pen, 16, y, btn.Width - 16, y);
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
                page.AutoScroll = true;
                page.Padding = new Padding(24, 20, 24, 20);
                Controls.Add(page);
            }
            _pages[0].Visible = true;

            Controls.Add(_tabBar);

            LoadSettings();
            FormClosing += SettingsForm_FormClosing;
        }

        // ===== Pages =====

        private Panel BuildMainPage()
        {
            var page = new Panel { BackColor = BgDark };

            // Header
            var title = MakeLabel("SmartTaskbar", 0, 0, true, 18F);
            page.Controls.Add(title);
            _lblStatus = MakeLabel("Status: Auto Mode OFF", 0, 32, false, 10F, TextSecondary);
            page.Controls.Add(_lblStatus);

            // Card: Taskbar behaviors
            var card = CreateCard(0, 70, 660, 0);
            page.Controls.Add(card);

            int y = 0;
            y = AddSettingsRow(card, y,
                "Auto Mode",
                "Automatically hide the taskbar when the focused window intersects it.",
                out _togMainAuto);
            _togMainAuto.CheckedChanged += (_, _) =>
            {
                UserSettings.AutoModeType = _togMainAuto.Checked ? AutoModeType.Auto : AutoModeType.None;
                if (_togAutoMode != null) _togAutoMode.Checked = _togMainAuto.Checked;
                _lblStatus.Text = _togMainAuto.Checked ? "Status: Auto Mode ON" : "Status: Auto Mode OFF";
            };

            y = AddDivider(card, y);
            y = AddSettingsRow(card, y,
                "Run at Windows startup",
                "Start SmartTaskbar when you sign in to Windows.",
                out _togMainStartup);
            _togMainStartup.CheckedChanged += (_, _) =>
            {
                StartupHelper.SetRunAtStartup(_togMainStartup.Checked);
                if (_togStartup != null) _togStartup.Checked = _togMainStartup.Checked;
            };

            card.Height = y + 8;

            var tip = MakeLabel("Closing this window keeps SmartTaskbar running in the system tray.", 0, card.Bottom + 16, false, 9F, TextSecondary);
            page.Controls.Add(tip);

            return page;
        }

        private Panel BuildAnimationPage()
        {
            var page = new Panel { BackColor = BgDark };

            page.Controls.Add(MakeLabel("Animation", 0, 0, true, 18F));
            page.Controls.Add(MakeLabel("Control taskbar animation behavior.", 0, 32, false, 10F, TextSecondary));

            var card = CreateCard(0, 70, 660, 0);
            page.Controls.Add(card);

            int y = AddSettingsRow(card, 0,
                "Taskbar animation",
                "Enable or disable the system taskbar animation effect.",
                out _togAnimation);
            _togAnimation.CheckedChanged += (_, _) =>
            {
                if (_togAnimation.Checked != Fun.IsEnableTaskbarAnimation())
                    Fun.ChangeTaskbarAnimation();
            };

            card.Height = y + 8;
            return page;
        }

        private Panel BuildStylePage()
        {
            var page = new Panel { BackColor = BgDark };

            page.Controls.Add(MakeLabel("Style", 0, 0, true, 18F));
            page.Controls.Add(MakeLabel("Taskbar behavior and exit options.", 0, 32, false, 10F, TextSecondary));

            var card = CreateCard(0, 70, 660, 0);
            page.Controls.Add(card);

            int y = AddSettingsRow(card, 0,
                "Auto Mode",
                "Hide the taskbar when the focused window intersects it.",
                out _togAutoMode);
            _togAutoMode.CheckedChanged += (_, _) =>
            {
                UserSettings.AutoModeType = _togAutoMode.Checked ? AutoModeType.Auto : AutoModeType.None;
                if (_togMainAuto != null) _togMainAuto.Checked = _togAutoMode.Checked;
                if (_lblStatus != null)
                    _lblStatus.Text = _togAutoMode.Checked ? "Status: Auto Mode ON" : "Status: Auto Mode OFF";
            };

            y = AddDivider(card, y);
            y = AddSettingsRow(card, y,
                "Show taskbar on exit",
                "Restore the taskbar when SmartTaskbar exits.",
                out _togShowOnExit);
            _togShowOnExit.CheckedChanged += (_, _) =>
            {
                UserSettings.ShowTaskbarWhenExit = _togShowOnExit.Checked;
            };

            card.Height = y + 8;
            return page;
        }

        private Panel BuildStartupPage()
        {
            var page = new Panel { BackColor = BgDark };

            page.Controls.Add(MakeLabel("Startup", 0, 0, true, 18F));
            page.Controls.Add(MakeLabel("Launch options when Windows starts.", 0, 32, false, 10F, TextSecondary));

            var card = CreateCard(0, 70, 660, 0);
            page.Controls.Add(card);

            int y = AddSettingsRow(card, 0,
                "Run at Windows startup",
                "Start SmartTaskbar automatically when you sign in. No admin rights required.",
                out _togStartup);
            _togStartup.CheckedChanged += (_, _) =>
            {
                StartupHelper.SetRunAtStartup(_togStartup.Checked);
                if (_togMainStartup != null) _togMainStartup.Checked = _togStartup.Checked;
            };

            card.Height = y + 8;
            return page;
        }

        // ===== Helpers (Win11 Settings row style) =====

        private static int AddSettingsRow(Panel card, int y, string title, string description, out ToggleSwitch toggle)
        {
            const int rowH = 72;
            const int padX = 20;

            var titleLbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11F),
                ForeColor = TextPrimary,
                Location = new Point(padX, y + 14),
                AutoSize = true
            };
            card.Controls.Add(titleLbl);

            var descLbl = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextSecondary,
                Location = new Point(padX, y + 40),
                Size = new Size(520, 24)
            };
            card.Controls.Add(descLbl);

            toggle = new ToggleSwitch
            {
                Location = new Point(card.Width - 44 - padX, y + (rowH - 24) / 2),
                Size = new Size(44, 24)
            };
            card.Controls.Add(toggle);

            return y + rowH;
        }

        private static int AddDivider(Panel card, int y)
        {
            var line = new Panel
            {
                BackColor = Divider,
                Location = new Point(16, y),
                Size = new Size(card.Width - 32, 1)
            };
            card.Controls.Add(line);
            return y + 1;
        }

        private static Label MakeLabel(string text, int x, int y, bool bold, float size, Color? color = null)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color ?? TextPrimary,
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private static Panel CreateCard(int x, int y, int w, int h)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, Math.Max(h, 40)),
                BackColor = CardBg
            };

            card.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = RoundedRect(rect, 8);
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

                if (_togMainAuto != null) _togMainAuto.Checked = auto;
                if (_togMainStartup != null) _togMainStartup.Checked = startup;
                if (_togAnimation != null) _togAnimation.Checked = anim;
                if (_togAutoMode != null) _togAutoMode.Checked = auto;
                if (_togShowOnExit != null) _togShowOnExit.Checked = showExit;
                if (_togStartup != null) _togStartup.Checked = startup;

                if (_lblStatus != null)
                    _lblStatus.Text = auto ? "Status: Auto Mode ON" : "Status: Auto Mode OFF";
            }
            catch { }
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
