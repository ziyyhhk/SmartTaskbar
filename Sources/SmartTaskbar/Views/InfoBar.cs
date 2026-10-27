using System.Drawing.Drawing2D;

namespace SmartTaskbar
{
    /// <summary>
    ///     Rounded informational banner with an accent info glyph (Windows 11 info bar style).
    /// </summary>
    internal sealed class InfoBar : Panel
    {
        private const int Radius = 8;
        private const int IconLeft = 16;
        private const int IconWidth = 20;
        private const int TextLeft = 44;
        private const int TextRight = 16;
        private const int PadY = 12;
        private const int MinHeight = 48;

        private readonly Label _icon;
        private readonly Label _text;
        private int _width = 480;

        public InfoBar(string message)
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
            BackColor = Win11Theme.Background;

            _icon = new Label
            {
                Text = "\uE946", // Segoe MDL2 Assets: Info
                Font = Win11Theme.IconFont,
                ForeColor = Win11Theme.Accent,
                BackColor = Win11Theme.Surface,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(IconLeft, 0)
            };

            _text = new Label
            {
                Text = message,
                Font = Win11Theme.BodyFont,
                ForeColor = Win11Theme.TextPrimary,
                BackColor = Win11Theme.Surface,
                Location = new Point(TextLeft, PadY)
            };

            Controls.Add(_icon);
            Controls.Add(_text);
            Fit(480);
        }

        /// <summary>
        ///     Replaces the message and re-wraps it at the current width.
        /// </summary>
        public void SetText(string message)
        {
            if (_text.Text == message) return;
            _text.Text = message;
            Fit(_width);
        }

        /// <summary>
        ///     Resizes the bar to a width and re-wraps its text.
        /// </summary>
        public void Fit(int width)
        {
            _width = width;
            int textWidth = Math.Max(1, width - TextLeft - TextRight);
            int textHeight = TextRenderer.MeasureText(
                _text.Text,
                _text.Font,
                new Size(textWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;

            int height = Math.Max(MinHeight, textHeight + PadY * 2);

            Size = new Size(width, height);
            _icon.Size = new Size(IconWidth, height);
            _text.Size = new Size(textWidth, textHeight);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = Win11Theme.RoundedRectangle(new RectangleF(0F, 0F, Width - 1F, Height - 1F), Radius);
            using var brush = new SolidBrush(Win11Theme.Surface);
            e.Graphics.FillPath(brush, path);
        }
    }
}
