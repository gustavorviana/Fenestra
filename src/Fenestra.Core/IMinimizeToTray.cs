namespace Fenestra.Core;

/// <summary>
/// Marker interface for windows that should minimize to the system tray
/// instead of closing when the user clicks the close button.
/// Requires <c>services.AddWpfTrayIcon()</c> and <c>services.AddWpfMinimizeToTray()</c>.
/// </summary>
public interface IMinimizeToTray
{
}
