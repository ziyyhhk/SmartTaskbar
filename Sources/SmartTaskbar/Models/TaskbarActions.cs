namespace SmartTaskbar
{
    /// <summary>
    ///     Callbacks the settings window uses to change taskbar behavior. The tray owns the side effects.
    /// </summary>
    internal sealed record TaskbarActions(
        Action<bool> SetEnabled,
        Action<bool> SetAutoMode,
        Action<bool> SetCenter,
        Action ApplyCenterOptions,
        Func<string> GetCenterStatus);
}
