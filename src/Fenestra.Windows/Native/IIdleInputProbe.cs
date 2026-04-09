namespace Fenestra.Windows.Native;

/// <summary>
/// Thin abstraction over <c>GetLastInputInfo</c> + <c>GetTickCount</c>.
/// Internal test seam — mockable for idle state machine testing without touching user32.
/// </summary>
internal interface IIdleInputProbe
{
    /// <summary>
    /// Returns how long since the last global user input.
    /// Returns <see cref="TimeSpan.Zero"/> if the native call fails (treated as "active").
    /// </summary>
    TimeSpan GetIdleTime();
}
