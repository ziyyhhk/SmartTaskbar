using System.Drawing.Drawing2D;

namespace SmartTaskbar
{
    /// <summary>
    /// Shared Windows 11 dark Settings palette and drawing helpers.
    /// </summary>
    internal static class Win11Theme
    {
        public static readonly Color Bg = Color.FromArgb(32, 32, 32);
        public static readonly Color Card = Color.FromArgb(43, 43, 43);
        public static readonly Color CardHover = Color.FromArgb(50, 50, 50);
        public static readonly Color Border = Color.FromArgb(55, 55, 55);
        public static readonly Color Divider = Color.FromArgb(55, 55, 55);
        public static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
        public static readonly Color TextSecondary = Color.FromArgb(190, 190, 190);
        public static readonly Color Accent = Color.FromArgb(96, 205, 255);
        public static readonly Color ToggleOff = Color.FromArgb(90, 90, 90);
        public static readonly Color ToggleOn = Color.FromArgb(96, 205, 255);

        public static Font TitleFont => new Font("Segoe UI", 18F, FontStyle.Bold);
        public static Font SectionFont => new Font("Segoe UI", 11F, FontStyle.Regular);
        public static Font BodyFont => new Font("Segoe UI", 10F, FontStyle.Regular);
        public static Font CaptionFont => new Font("Segoe UI", 9F, FontStyle.Regular);
        public static Font TabFont => new Font("Segoe UI Semibold", 10F, FontStyle.Regular);

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = Math.Max(1, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawCard(Graphics g, Rectangle rect, int radius = 8)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(rect, radius);
            using var brush = new SolidBrush(Card);
            using var pen = new Pen(Border);
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }
    }
}
