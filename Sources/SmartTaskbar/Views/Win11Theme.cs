using System.Drawing.Drawing2D;

namespace SmartTaskbar
{
    /// <summary>
    ///     Windows 11 dark palette, typography and drawing helpers used by the settings window.
    /// </summary>
    internal static class Win11Theme
    {
        // ----- Surfaces -----
        public static readonly Color Background = Color.FromArgb(32, 32, 32);      // #202020 window
        public static readonly Color Surface = Color.FromArgb(43, 43, 43);         // #2B2B2B card
        public static readonly Color SurfaceHover = Color.FromArgb(50, 50, 50);    // #323232 hovered tab
        public static readonly Color Divider = Color.FromArgb(60, 60, 60);         // #3C3C3C row divider

        // ----- Accent -----
        public static readonly Color Accent = Color.FromArgb(96, 205, 255);        // #60CDFF
        public static readonly Color AccentHover = Color.FromArgb(138, 221, 255);  // #8ADDFF

        // ----- Text -----
        public static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
        public static readonly Color TextSecondary = Color.FromArgb(200, 200, 200); // #C8C8C8
        public static readonly Color FocusRing = Color.White;

        // ----- Toggle switch -----
        public static readonly Color ToggleBorder = Color.FromArgb(160, 160, 160);
        public static readonly Color ToggleBorderHover = Color.FromArgb(190, 190, 190);
        public static readonly Color ToggleKnob = Color.FromArgb(200, 200, 200);
        public static readonly Color ToggleKnobHover = Color.FromArgb(230, 230, 230);
        public static readonly Color ToggleKnobOn = Color.FromArgb(0, 0, 0);
        public static readonly Color ToggleDisabledBorder = Color.FromArgb(80, 80, 80);
        public static readonly Color ToggleDisabledKnob = Color.FromArgb(96, 96, 96);
        public static readonly Color ToggleDisabledOn = Color.FromArgb(77, 77, 77);

        // ----- Typography (Segoe UI is installed on Windows 10 and 11) -----
        public static readonly Font TitleFont = new("Segoe UI Semibold", 20F);
        public static readonly Font SectionFont = new("Segoe UI Semibold", 10F);
        public static readonly Font BodyFont = new("Segoe UI", 10F);
        public static readonly Font CaptionFont = new("Segoe UI", 9F);
        public static readonly Font IconFont = new("Segoe MDL2 Assets", 10F);

        /// <summary>
        ///     Rounded rectangle path (used for cards, tracks and pills).
        /// </summary>
        public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
        {
            float d = radius * 2F;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180F, 90F);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270F, 90F);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0F, 90F);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90F, 90F);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        ///     Linear blend between two colors. t = 0 returns from, t = 1 returns to.
        /// </summary>
        public static Color Mix(Color from, Color to, float t)
        {
            t = Math.Clamp(t, 0F, 1F);
            return Color.FromArgb(
                (int)Math.Round(from.A + (to.A - from.A) * t),
                (int)Math.Round(from.R + (to.R - from.R) * t),
                (int)Math.Round(from.G + (to.G - from.G) * t),
                (int)Math.Round(from.B + (to.B - from.B) * t));
        }
    }
}
