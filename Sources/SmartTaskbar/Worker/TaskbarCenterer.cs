using System.Diagnostics;
using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;

namespace SmartTaskbar
{
    /// <summary>
    ///     Easing curves for the icon-centering animation. Public because UserSettings exposes it.
    /// </summary>
    public enum CenterEasing
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        Overshoot,
        Bounce
    }

    /// <summary>
    ///     Options shown in the settings window, in the same order as the combo boxes.
    /// </summary>
    internal static class CenterOptions
    {
        public static readonly CenterEasing[] Easings =
        {
            CenterEasing.Linear,
            CenterEasing.EaseIn,
            CenterEasing.EaseOut,
            CenterEasing.EaseInOut,
            CenterEasing.Overshoot,
            CenterEasing.Bounce
        };

        public static readonly string[] EasingNames = { "Linear", "Ease in", "Ease out", "Ease in-out", "Overshoot", "Bounce" };

        public static readonly string[] SpeedNames = { "Fast", "Normal", "Slow" };

        public static readonly int[] SpeedMs = { 200, 350, 600 };

        /// <summary>
        ///     Maps linear progress t (0..1) to eased progress.
        /// </summary>
        public static double Apply(CenterEasing easing, double t)
        {
            return easing switch
            {
                CenterEasing.Linear => t,
                CenterEasing.EaseIn => t * t * t,
                CenterEasing.EaseOut => 1 - Math.Pow(1 - t, 3),
                CenterEasing.EaseInOut => t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2,
                CenterEasing.Overshoot => 1 + 2.70158 * Math.Pow(t - 1, 3) + 1.70158 * Math.Pow(t - 1, 2),
                CenterEasing.Bounce => Bounce(t),
                _ => t
            };
        }

        private static double Bounce(double t)
        {
            const double n = 7.5625;
            const double d = 2.75;

            if (t < 1 / d) return n * t * t;
            if (t < 2 / d)
            {
                t -= 1.5 / d;
                return n * t * t + 0.75;
            }
            if (t < 2.5 / d)
            {
                t -= 2.25 / d;
                return n * t * t + 0.9375;
            }
            t -= 2.625 / d;
            return n * t * t + 0.984375;
        }
    }

    /// <summary>
    ///     Windows 10 icon centering. Moves the taskbar's task-list window to the middle of the screen
    ///     and eases it there. Explorer resets the layout when icons change, so the position is re-checked
    ///     every tick. Stopping restores the original position. Windows 11 uses its native alignment instead.
    ///     <see cref="Status" /> explains what it is doing, which helps when reporting a problem.
    /// </summary>
    internal sealed class TaskbarCenterer
    {
        private const string ShellClass = "Shell_TrayWnd";
        private const string RebarClass = "ReBarWindow32";
        private const string TaskSwClass = "MSTaskSwWClass";
        private const string TaskListClass = "MSTaskListWClass";

        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;

        private const int TickMs = 15;
        private const int RefreshMs = 500;
        private const int ExplorerMoveToleranceX = 2;

        private enum LayoutState { Ok, Vertical, Unreadable, TooWide }

        private readonly record struct TaskbarLayout(int TargetX, int CurrentX, int Y, int MaxX, int ListWidth);

        [StructLayout(LayoutKind.Sequential)]
        private struct WinRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindWindow(string? className, string? windowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? windowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out WinRect rect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

        private readonly Timer _timer;
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        private IntPtr _shell;
        private IntPtr _taskSw;
        private IntPtr _taskList;

        private CenterEasing _easing = CenterEasing.EaseOut;
        private int _durationMs = 350;

        private bool _captured;          // original task-list position recorded
        private int _originalX;

        private bool _hasTarget;         // an animation target exists
        private int _fromX;
        private int _toX;
        private long _animStartMs;
        private int _animDurationMs;
        private bool _animating;

        private bool _hasLastSet;
        private int _lastSetX;
        private long _nextRefreshMs;

        public TaskbarCenterer()
        {
            _timer = new Timer { Interval = TickMs };
            _timer.Tick += (_, _) => Tick();
        }

        /// <summary>
        ///     Human-readable state, shown in the settings window.
        /// </summary>
        public string Status { get; private set; } = "Off";

        public bool IsRunning => _timer.Enabled;

        public void Configure(CenterEasing easing, int durationMs)
        {
            _easing = easing;
            _durationMs = Math.Max(0, durationMs);
        }

        public void Start()
        {
            if (_timer.Enabled) return;

            Status = "Starting...";
            _timer.Start();
        }

        /// <summary>
        ///     Stops the engine and moves the icons back to where they were before centering.
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
            RestoreOriginal();
            Status = "Off";
        }

        private void Tick()
        {
            try
            {
                long now = _clock.ElapsedMilliseconds;

                if (now >= _nextRefreshMs)
                {
                    _nextRefreshMs = now + RefreshMs;
                    ResolveWindows();
                }

                if (_taskList == IntPtr.Zero || _taskSw == IntPtr.Zero)
                {
                    Status = "Taskbar icon list not found. Windows 10 layout was not detected.";
                    return;
                }

                LayoutState state = ReadLayout(out TaskbarLayout layout);
                if (state != LayoutState.Ok)
                {
                    Status = Describe(state);
                    return;
                }

                if (!_captured)
                {
                    _originalX = layout.CurrentX;
                    _captured = true;
                }

                if (!_hasTarget || layout.TargetX != _toX)
                    BeginAnimation(layout.CurrentX, layout.TargetX);
                else if (_hasLastSet && Math.Abs(layout.CurrentX - _lastSetX) > ExplorerMoveToleranceX)
                    BeginAnimation(layout.CurrentX, _toX);     // Explorer moved the icons: ease back
                else if (!_animating && layout.CurrentX != _toX)
                    BeginAnimation(layout.CurrentX, _toX);

                int x = _toX;
                if (_animating)
                {
                    double t = Math.Clamp((now - _animStartMs) / (double)_animDurationMs, 0.0, 1.0);
                    if (t >= 1.0)
                    {
                        _animating = false;
                    }
                    else
                    {
                        double eased = CenterOptions.Apply(_easing, t);
                        x = (int)Math.Round(_fromX + (_toX - _fromX) * eased);
                    }
                }

                x = Math.Clamp(x, 0, layout.MaxX);
                if (x != layout.CurrentX && !MoveTo(x, layout.Y))
                {
                    Status = $"Moving the icons failed (Windows error {Marshal.GetLastWin32Error()}).";
                    return;
                }

                Status = _animating
                    ? "Easing the icons into the center..."
                    : $"Centered. Icon strip is {layout.ListWidth}px wide.";
            }
            catch (Exception ex)
            {
                Status = "Error: " + ex.Message;
            }
        }

        private void BeginAnimation(int from, int to)
        {
            _hasTarget = true;
            _fromX = from;
            _toX = to;
            _animStartMs = _clock.ElapsedMilliseconds;
            _animDurationMs = _durationMs;
            _animating = _durationMs > 0 && from != to;
        }

        private bool MoveTo(int x, int y)
        {
            if (!SetWindowPos(_taskList, IntPtr.Zero, x, y, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate))
                return false;

            _lastSetX = x;
            _hasLastSet = true;
            return true;
        }

        /// <summary>
        ///     Finds the taskbar windows. Explorer can restart, which changes the handles.
        /// </summary>
        private void ResolveWindows()
        {
            IntPtr shell = FindWindow(ShellClass, null);
            IntPtr rebar = shell == IntPtr.Zero ? IntPtr.Zero : FindWindowEx(shell, IntPtr.Zero, RebarClass, null);

            // Normally the task-band window lives inside the rebar; fall back to a direct child of the taskbar.
            IntPtr taskSw = rebar != IntPtr.Zero ? FindWindowEx(rebar, IntPtr.Zero, TaskSwClass, null) : IntPtr.Zero;
            if (taskSw == IntPtr.Zero && shell != IntPtr.Zero)
                taskSw = FindWindowEx(shell, IntPtr.Zero, TaskSwClass, null);

            IntPtr taskList = taskSw == IntPtr.Zero ? IntPtr.Zero : FindWindowEx(taskSw, IntPtr.Zero, TaskListClass, null);

            if (taskList != _taskList)
                ResetState();

            _shell = shell;
            _taskSw = taskSw;
            _taskList = taskList;
        }

        private void ResetState()
        {
            _captured = false;
            _hasTarget = false;
            _animating = false;
            _hasLastSet = false;
        }

        /// <summary>
        ///     Reads the taskbar layout and works out where the task list should sit to be centered on screen.
        /// </summary>
        private LayoutState ReadLayout(out TaskbarLayout layout)
        {
            layout = default;

            if (!GetRect(_shell, out WinRect shell) || !GetRect(_taskSw, out WinRect sw) || !GetRect(_taskList, out WinRect list))
                return LayoutState.Unreadable;

            // Only horizontal taskbars (bottom or top) are supported.
            if (shell.Right - shell.Left < shell.Bottom - shell.Top)
                return LayoutState.Vertical;

            int listWidth = list.Right - list.Left;
            int maxX = (sw.Right - sw.Left) - listWidth;
            if (maxX < 0)
                return LayoutState.TooWide;

            Rectangle screen = Screen.FromHandle(_shell).Bounds;
            int desiredScreenLeft = screen.Left + (screen.Width - listWidth) / 2;

            layout = new TaskbarLayout(
                TargetX: Math.Clamp(desiredScreenLeft - sw.Left, 0, maxX),
                CurrentX: list.Left - sw.Left,
                Y: list.Top - sw.Top,
                MaxX: maxX,
                ListWidth: listWidth);
            return LayoutState.Ok;
        }

        private static string Describe(LayoutState state) => state switch
        {
            LayoutState.Vertical => "Only a bottom or top taskbar can be centered.",
            LayoutState.TooWide => "The icons are wider than the taskbar, so they can't be centered.",
            _ => "Could not read the taskbar window positions."
        };

        private void RestoreOriginal()
        {
            if (_captured && ReadLayout(out TaskbarLayout layout) == LayoutState.Ok)
                MoveTo(Math.Clamp(_originalX, 0, layout.MaxX), layout.Y);

            ResetState();
        }

        private static bool GetRect(IntPtr hWnd, out WinRect rect)
        {
            rect = default;
            return hWnd != IntPtr.Zero && GetWindowRect(hWnd, out rect);
        }
    }
}
