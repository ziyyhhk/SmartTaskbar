using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SmartTaskbar
{
    /// <summary>
    /// Simple Windows 11 style toggle switch for WinForms.
    /// </summary>
    internal sealed class ToggleSwitch : Control
    {
        private bool _checked;
        private bool _hover;

        private static readonly Color OffTrack = Color.FromArgb(90, 90, 90);
        private static readonly Color OnTrack = Color.FromArgb(96, 205, 255);
        private static readonly Color Knob = Color.White;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.SupportsTransparentBackColor, true);
            Size = new Size(44, 24);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        [DefaultValue(false)]
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? CheckedChanged;

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var trackRect = new Rectangle(0, 2, Width - 1, Height - 5);
            Color trackColor = _checked ? OnTrack : OffTrack;
            if (_hover)
                trackColor = _checked
                    ? Color.FromArgb(120, 215, 255)
                    : Color.FromArgb(110, 110, 110);

            using (var path = RoundedRect(trackRect, trackRect.Height / 2))
            using (var brush = new SolidBrush(trackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            int knobSize = Height - 10;
            int knobX = _checked ? Width - knobSize - 5 : 5;
            int knobY = (Height - knobSize) / 2;

            using (var brush = new SolidBrush(Knob))
            {
                e.Graphics.FillEllipse(brush, knobX, knobY, knobSize, knobSize);
            }
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
    }
}
