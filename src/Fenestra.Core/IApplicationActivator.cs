namespace Fenestra.Core;

/// <summary>
/// Brings the application to the foreground, restoring it if minimized or hidden.
/// </summary>
public interface IApplicationActivator
{
    /// <summary>
    /// Activates and brings the main application window to the foreground.
    /// </summary>
    void BringToForeground();
}
