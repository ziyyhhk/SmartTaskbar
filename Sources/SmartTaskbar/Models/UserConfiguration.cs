namespace SmartTaskbar
{
    /// <summary>
    ///     User settings configuration
    /// </summary>
    internal struct UserConfiguration
    {
        /// <summary>
        ///     Auto mode type
        /// </summary>
        public AutoModeType AutoModeType { get; set; }

        /// <summary>
        ///     Show taskbar when exiting
        /// </summary>
        public bool ShowTaskbarWhenExit { get; set; }

        /// <summary>
        ///     Master switch: when false, SmartTaskbar leaves the taskbar alone
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///     Center the taskbar icons (Windows 10 engine; Windows 11 uses the native setting)
        /// </summary>
        public bool CenterIcons { get; set; }

        /// <summary>
        ///     Easing curve used when the icons slide into the center
        /// </summary>
        public CenterEasing CenterEasing { get; set; }

        /// <summary>
        ///     Duration of the centering animation in milliseconds
        /// </summary>
        public int CenterDurationMs { get; set; }
    }
}
