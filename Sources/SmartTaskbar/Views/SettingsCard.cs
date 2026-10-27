using System.Drawing.Drawing2D;

namespace SmartTaskbar
{
    /// <summary>
    ///     Rounded Windows 11 style card. Each row shows a title and short description on the left
    ///     and a <see cref="ToggleSwitch" /> on the right. Thin dividers separate rows, and clicking
    ///     anywhere on a row flips its switch.
    /// </summary>
    internal sealed class SettingsCard : Control
    {
        private const int CornerRadius = 8;
        private const int PadX = 20;
        private const int PadY = 14;
        private const int DescriptionGap = 2;
        private const int ToggleGap = 24;
        private const int MinRowHeight = 60;

        private static readonly TextFormatFlags TextFlags =
            TextFormatFlags.Left
            | TextFormatFlags.Top
            | TextFormatFlags.WordBreak
            | TextFormatFlags.NoPrefix
            | TextFormatFlags.NoPadding;

        private sealed class Row
        {
            public Row(string title, string description, ToggleSwitch toggle)
            {
                Title = title;
                Description = description;
                Toggle = toggle;
            }

            public string Title { get; }
            public string Description { get; }
            public ToggleSwitch Toggle { get; }
            public int Top { get; set; }
            public int Height { get; set; }
            public int TitleHeight { get; set; }
            public int DescriptionHeight { get; set; }
        }

        private readonly List<Row> _rows = new();
        private int _layoutWidth = -1;

        public SettingsCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw, true);

            // Outside the rounded corners the card shows the window background.
            BackColor = Win11Theme.Background;
        }

        /// <summary>
        ///     Adds a settings row and returns its toggle switch so the caller can wire up events.
        /// </summary>
        public ToggleSwitch AddRow(string title, string description)
        {
            var toggle = new ToggleSwitch
            {
                AccessibleName = title,
                AccessibleDescription = description
            };

            _rows.Add(new Row(title, description, toggle));
            Controls.Add(toggle);
            Relayout();
            return toggle;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width != _layoutWidth)
                Relayout();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;

            foreach (var row in _rows)
            {
                if (e.Y >= row.Top && e.Y < row.Top + row.Height)
                {
                    row.Toggle.Toggle();
                    return;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = Win11Theme.RoundedRectangle(new RectangleF(0F, 0F, Width - 1F, Height - 1F), CornerRadius))
            using (var brush = new SolidBrush(Win11Theme.Surface))
            {
                g.FillPath(brush, path);
            }

            // Dividers between rows (none above the first row).
            using (var divider = new Pen(Win11Theme.Divider, 1F))
            {
                for (int i = 1; i < _rows.Count; i++)
                {
                    int y = _rows[i].Top;
                    g.DrawLine(divider, PadX, y, Width - PadX, y);
                }
            }

            int textWidth = TextWidth();
            foreach (var row in _rows)
            {
                int contentHeight = row.TitleHeight + (row.DescriptionHeight > 0 ? DescriptionGap + row.DescriptionHeight : 0);
                int top = row.Top + (row.Height - contentHeight) / 2;

                TextRenderer.DrawText(g, row.Title, Win11Theme.BodyFont,
                                      new Rectangle(PadX, top, textWidth, row.TitleHeight),
                                      Win11Theme.TextPrimary, TextFlags);

                if (row.DescriptionHeight > 0)
                {
                    TextRenderer.DrawText(g, row.Description, Win11Theme.CaptionFont,
                                          new Rectangle(PadX, top + row.TitleHeight + DescriptionGap, textWidth, row.DescriptionHeight),
                                          Win11Theme.TextSecondary, TextFlags);
                }
            }
        }

        private int TextWidth()
            => Math.Max(1, Width - PadX * 2 - ToggleSwitch.TrackWidth - ToggleGap);

        /// <summary>
        ///     Measures every row for the current width, positions its toggle and sizes the card.
        /// </summary>
        private void Relayout()
        {
            _layoutWidth = Width;

            int textWidth = TextWidth();
            int y = 0;

            foreach (var row in _rows)
            {
                row.TitleHeight = MeasureHeight(row.Title, Win11Theme.BodyFont, textWidth);
                row.DescriptionHeight = string.IsNullOrEmpty(row.Description)
                    ? 0
                    : MeasureHeight(row.Description, Win11Theme.CaptionFont, textWidth);

                int contentHeight = row.TitleHeight + (row.DescriptionHeight > 0 ? DescriptionGap + row.DescriptionHeight : 0);

                row.Top = y;
                row.Height = Math.Max(MinRowHeight, contentHeight + PadY * 2);
                row.Toggle.Location = new Point(
                    Width - PadX - ToggleSwitch.TrackWidth,
                    row.Top + (row.Height - ToggleSwitch.TrackHeight) / 2);

                y += row.Height;
            }

            Height = y;
            Invalidate();
        }

        private static int MeasureHeight(string text, Font font, int width)
            => TextRenderer.MeasureText(text, font, new Size(width, int.MaxValue), TextFlags).Height;
    }
}
