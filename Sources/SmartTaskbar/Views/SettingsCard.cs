using System.Drawing.Drawing2D;

namespace SmartTaskbar
{
    /// <summary>
    ///     Rounded Windows 11 style card. Each row shows a title and short description on the left and
    ///     an accessory on the right: a toggle switch (clicking anywhere on the row flips it) or a dropdown.
    ///     Thin dividers separate rows.
    /// </summary>
    internal sealed class SettingsCard : Control
    {
        private const int CornerRadius = 8;
        private const int PadX = 20;
        private const int PadY = 14;
        private const int DescriptionGap = 2;
        private const int ToggleGap = 24;
        private const int MinRowHeight = 60;
        private const int ChoiceWidth = 170;

        private static readonly TextFormatFlags TextFlags =
            TextFormatFlags.Left
            | TextFormatFlags.Top
            | TextFormatFlags.WordBreak
            | TextFormatFlags.NoPrefix
            | TextFormatFlags.NoPadding;

        private sealed class Row
        {
            public Row(string title, string description, Control accessory, ToggleSwitch? toggle)
            {
                Title = title;
                Description = description;
                Accessory = accessory;
                Toggle = toggle;
            }

            public string Title { get; }
            public string Description { get; }
            public Control Accessory { get; }
            public ToggleSwitch? Toggle { get; }
            public int Top { get; set; }
            public int Height { get; set; }
            public int TextWidth { get; set; }
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
        ///     Adds a row with a toggle switch and returns the switch.
        /// </summary>
        public ToggleSwitch AddRow(string title, string description)
        {
            var toggle = new ToggleSwitch
            {
                AccessibleName = title,
                AccessibleDescription = description
            };

            AddRowInternal(new Row(title, description, toggle, toggle));
            return toggle;
        }

        /// <summary>
        ///     Adds a row with a dropdown on the right and returns it.
        /// </summary>
        public ComboBox AddChoiceRow(string title, string description, string[] items, int selectedIndex)
        {
            var combo = CreateDarkCombo(items, selectedIndex);
            combo.AccessibleName = title;
            combo.AccessibleDescription = description;

            AddRowInternal(new Row(title, description, combo, null));
            return combo;
        }

        private void AddRowInternal(Row row)
        {
            _rows.Add(row);
            Controls.Add(row.Accessory);
            Relayout();
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
                    row.Toggle?.Toggle();
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

            foreach (var row in _rows)
            {
                int contentHeight = row.TitleHeight + (row.DescriptionHeight > 0 ? DescriptionGap + row.DescriptionHeight : 0);
                int top = row.Top + (row.Height - contentHeight) / 2;

                TextRenderer.DrawText(g, row.Title, Win11Theme.BodyFont,
                                      new Rectangle(PadX, top, row.TextWidth, row.TitleHeight),
                                      Win11Theme.TextPrimary, TextFlags);

                if (row.DescriptionHeight > 0)
                {
                    TextRenderer.DrawText(g, row.Description, Win11Theme.CaptionFont,
                                          new Rectangle(PadX, top + row.TitleHeight + DescriptionGap, row.TextWidth, row.DescriptionHeight),
                                          Win11Theme.TextSecondary, TextFlags);
                }
            }
        }

        /// <summary>
        ///     Measures every row for the current width, positions its accessory and sizes the card.
        /// </summary>
        private void Relayout()
        {
            _layoutWidth = Width;

            int y = 0;
            foreach (var row in _rows)
            {
                row.TextWidth = Math.Max(1, Width - PadX * 2 - row.Accessory.Width - ToggleGap);
                row.TitleHeight = MeasureHeight(row.Title, Win11Theme.BodyFont, row.TextWidth);
                row.DescriptionHeight = string.IsNullOrEmpty(row.Description)
                    ? 0
                    : MeasureHeight(row.Description, Win11Theme.CaptionFont, row.TextWidth);

                int contentHeight = row.TitleHeight + (row.DescriptionHeight > 0 ? DescriptionGap + row.DescriptionHeight : 0);

                row.Top = y;
                row.Height = Math.Max(MinRowHeight, contentHeight + PadY * 2);
                row.Accessory.Location = new Point(
                    Width - PadX - row.Accessory.Width,
                    row.Top + (row.Height - row.Accessory.Height) / 2);

                y += row.Height;
            }

            Height = y;
            Invalidate();
        }

        private static int MeasureHeight(string text, Font font, int width)
            => TextRenderer.MeasureText(text, font, new Size(width, int.MaxValue), TextFlags).Height;

        /// <summary>
        ///     Dropdown styled for the dark surface. Items are owner-drawn so the list isn't white.
        /// </summary>
        private static ComboBox CreateDarkCombo(string[] items, int selectedIndex)
        {
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                DrawMode = DrawMode.OwnerDrawFixed,
                BackColor = Win11Theme.SurfaceHover,
                ForeColor = Win11Theme.TextPrimary,
                Font = Win11Theme.BodyFont,
                Width = ChoiceWidth
            };

            combo.Items.AddRange(items);
            combo.SelectedIndex = selectedIndex;

            combo.DrawItem += (_, e) =>
            {
                if (e.Index < 0) return;

                bool isEdit = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;
                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

                Color back = isEdit ? Win11Theme.SurfaceHover
                           : selected ? Win11Theme.SurfaceHover
                           : Win11Theme.Surface;

                using (var brush = new SolidBrush(back))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }

                Rectangle textRect = new(e.Bounds.Left + 10, e.Bounds.Top, e.Bounds.Width - 34, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, combo.GetItemText(combo.Items[e.Index]), combo.Font, textRect,
                                      Win11Theme.TextPrimary,
                                      TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                                      | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

                if (isEdit)
                {
                    Rectangle chevron = new(e.Bounds.Right - 26, e.Bounds.Top, 22, e.Bounds.Height);
                    TextRenderer.DrawText(e.Graphics, "\uE70D", Win11Theme.IconFont, chevron,
                                          Win11Theme.TextSecondary,
                                          TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                                          | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                }
            };

            return combo;
        }
    }
}
