namespace Fenestra.Core;

/// <summary>
/// Marker interface for windows that should minimize to the system tray
/// instead of closing when the user clicks the close button.
/// Requires UseTrayIcon() and UseMinimizeToTray() in the builder.
/// </summary>
public interface IMinimizeToTray
{
}
