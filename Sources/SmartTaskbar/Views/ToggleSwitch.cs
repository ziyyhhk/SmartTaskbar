using System.ComponentModel;
using System.Drawing.Drawing2D;
using Timer = System.Windows.Forms.Timer;

namespace SmartTaskbar
{
    /// <summary>
    ///     Windows 11 style toggle switch: 40 x 20 track, animated knob, hover/pressed/focus states,
    ///     mouse click and Space-bar support. The control paints its own <see cref="Win11Theme.Surface" />
    ///     background, so it should sit on a surface card.
    /// </summary>
    internal sealed class ToggleSwitch : Control
    {
        public const int TrackWidth = 40;
        public const int TrackHeight = 20;

        private const int KnobInset = 10;      // knob centre distance from each end of the track when off / on
        private const float KnobOffSize = 10F;
        private const float KnobOnSize = 14F;
        private const float HoverGrow = 2F;
        private const int AnimationIntervalMs = 15;
        private const float AnimationEase = 0.35F;

        private readonly Timer _timer;
        private bool _checked;
        private bool _hover;
        private bool _pressed;
        private float _position;               // 0 = off, 1 = on, eased between the two

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.Selectable, true);

            BackColor = Win11Theme.Surface;
            Size = new Size(TrackWidth, TrackHeight);
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.CheckButton;

            _timer = new Timer { Interval = AnimationIntervalMs };
            _timer.Tick += Timer_Tick;
        }

        [DefaultValue(false)]
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                _timer.Start();
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? CheckedChanged;

        /// <summary>
        ///     Flips the switch (used by mouse, keyboard and by the settings row click).
        /// </summary>
        public void Toggle()
        {
            if (!Enabled) return;
            Checked = !Checked;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            float target = _checked ? 1F : 0F;
            _position += (target - _position) * AnimationEase;

            if (Math.Abs(target - _position) < 0.01F)
            {
                _position = target;
                _timer.Stop();
            }

            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                _pressed = true;
                Focus();
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool wasPressed = _pressed;
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);

            if (wasPressed && e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
                Toggle();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && Enabled)
            {
                _pressed = true;
                Invalidate();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && _pressed)
            {
                _pressed = false;
                Invalidate();
                Toggle();
                e.Handled = true;
            }
            base.OnKeyUp(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            _pressed = false;
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float t = _position;

            Color accent = !Enabled ? Win11Theme.ToggleDisabledOn
                         : _hover ? Win11Theme.AccentHover
                         : Win11Theme.Accent;

            Color border = !Enabled ? Win11Theme.ToggleDisabledBorder
                         : _hover ? Win11Theme.ToggleBorderHover
                         : Win11Theme.ToggleBorder;

            Color fill = Win11Theme.Mix(BackColor, accent, t);
            Color stroke = Focused ? Win11Theme.FocusRing : Win11Theme.Mix(border, accent, t);

            var trackRect = new RectangleF(0.5F, 0.5F, Width - 1F, Height - 1F);
            using (var path = Win11Theme.RoundedRectangle(trackRect, trackRect.Height / 2F))
            using (var fillBrush = new SolidBrush(fill))
            using (var strokePen = new Pen(stroke, 1F))
            {
                g.FillPath(fillBrush, path);
                g.DrawPath(strokePen, path);
            }

            // Knob: small and grey when off, larger and dark on the accent track when on.
            float knobSize = Lerp(KnobOffSize, KnobOnSize, t) + (_hover && Enabled ? HoverGrow : 0F);
            float centerX = KnobInset + (Width - 2F * KnobInset) * t;
            float centerY = Height / 2F;

            Color knob = !Enabled
                ? Win11Theme.ToggleDisabledKnob
                : Win11Theme.Mix(_hover ? Win11Theme.ToggleKnobHover : Win11Theme.ToggleKnob,
                                 Win11Theme.ToggleKnobOn, t);

            using var knobBrush = new SolidBrush(knob);
            g.FillEllipse(knobBrush,
                          centerX - knobSize / 2F,
                          centerY - knobSize / 2F,
                          knobSize,
                          knobSize);
        }

        private static float Lerp(float from, float to, float t) => from + (to - from) * t;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
