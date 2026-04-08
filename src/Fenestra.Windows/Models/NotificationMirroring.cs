namespace Fenestra.Windows.Models;

/// <summary>
/// Specifies whether notification mirroring is allowed for cross-device scenarios.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.notificationmirroring
/// </remarks>
public enum NotificationMirroring
{
    Allowed = 0,
    Disabled = 1
}
