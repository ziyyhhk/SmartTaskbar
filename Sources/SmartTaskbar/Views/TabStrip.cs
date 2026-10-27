using System.Drawing.Drawing2D;

namespace SmartTaskbar
{
    /// <summary>
    ///     Horizontal tab bar in the Windows 11 Settings style: text tabs, a rounded accent
    ///     underline on the active tab, a subtle hover fill, and Left/Right arrow-key navigation.
    /// </summary>
    internal sealed class TabStrip : Control
    {
        private const int ItemPadX = 14;
        private const int ItemGap = 6;
        private const int IndicatorHeight = 3;
        private const int IndicatorBottomGap = 6;
        private const int HoverInsetY = 6;
        private const float HoverRadius = 6F;

        private static readonly TextFormatFlags TextFlags =
            TextFormatFlags.HorizontalCenter
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.SingleLine
            | TextFormatFlags.NoPadding
            | TextFormatFlags.NoPrefix;

        private readonly List<string> _items = new();
        private int _selected;
        private int _hover = -1;

        public TabStrip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.Selectable, true);

            BackColor = Win11Theme.Background;
            Font = Win11Theme.BodyFont;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PageTabList;
        }

        /// <summary>Horizontal distance from the left edge to the first tab.</summary>
        public int LeftInset { get; set; } = 32;

        public event EventHandler? SelectedIndexChanged;

        public int SelectedIndex
        {
            get => _selected;
            set
            {
                if (value < 0 || value >= _items.Count || value == _selected) return;
                _selected = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetItems(params string[] items)
        {
            _items.Clear();
            _items.AddRange(items);
            _selected = 0;
            _hover = -1;
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
            => keyData == Keys.Left || keyData == Keys.Right || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Left)
            {
                SelectedIndex = Math.Max(0, _selected - 1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right)
            {
                SelectedIndex = Math.Min(_items.Count - 1, _selected + 1);
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = IndexAt(e.Location);
            if (index != _hover)
            {
                _hover = index;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            int index = IndexAt(e.Location);
            if (index >= 0)
            {
                Focus();
                SelectedIndex = index;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            for (int i = 0; i < _items.Count; i++)
            {
                Rectangle r = ItemRect(i);
                bool active = i == _selected;

                if (i == _hover && !active)
                {
                    var pill = new RectangleF(r.X, r.Y + HoverInsetY, r.Width, r.Height - HoverInsetY * 2);
                    using var path = Win11Theme.RoundedRectangle(pill, HoverRadius);
                    using var hoverBrush = new SolidBrush(Win11Theme.SurfaceHover);
                    g.FillPath(hoverBrush, path);
                }

                Color textColor = active || i == _hover ? Win11Theme.TextPrimary : Win11Theme.TextSecondary;
                var textRect = new Rectangle(r.X, r.Y, r.Width, r.Height - IndicatorBottomGap - IndicatorHeight);
                TextRenderer.DrawText(g, _items[i], Font, textRect, textColor, TextFlags);

                if (active)
                {
                    var bar = new RectangleF(r.X + ItemPadX,
                                             r.Bottom - IndicatorBottomGap - IndicatorHeight,
                                             r.Width - ItemPadX * 2,
                                             IndicatorHeight);
                    using var barPath = Win11Theme.RoundedRectangle(bar, IndicatorHeight / 2F);
                    using var barBrush = new SolidBrush(Win11Theme.Accent);
                    g.FillPath(barBrush, barPath);
                }
            }

            // Focus cue for keyboard users: a thin outline around the active tab.
            if (Focused && _selected >= 0 && _selected < _items.Count)
            {
                Rectangle r = ItemRect(_selected);
                var ring = new RectangleF(r.X + 0.5F, r.Y + HoverInsetY + 0.5F, r.Width - 1F, r.Height - HoverInsetY * 2 - 1F);
                using var ringPath = Win11Theme.RoundedRectangle(ring, HoverRadius);
                using var ringPen = new Pen(Win11Theme.FocusRing, 1F);
                g.DrawPath(ringPen, ringPath);
            }

            // Divider along the bottom edge of the strip.
            using var line = new Pen(Win11Theme.Divider, 1F);
            g.DrawLine(line, 0, Height - 1, Width, Height - 1);
        }

        private int ItemWidth(string text)
            => TextRenderer.MeasureText(text, Font, new Size(int.MaxValue, int.MaxValue), TextFlags).Width + ItemPadX * 2;

        private Rectangle ItemRect(int index)
        {
            int x = LeftInset;
            for (int i = 0; i < _items.Count; i++)
            {
                int width = ItemWidth(_items[i]);
                if (i == index)
                    return new Rectangle(x, 0, width, Height);
                x += width + ItemGap;
            }
            return Rectangle.Empty;
        }

        private int IndexAt(Point point)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (ItemRect(i).Contains(point))
                    return i;
            }
            return -1;
        }
    }
}
