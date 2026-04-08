namespace Fenestra.Windows.Models;

/// <summary>
/// Specifies whether notifications are enabled for the app.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.notificationsetting
/// </remarks>
public enum NotificationSetting
{
    Enabled = 0,
    DisabledForApplication = 1,
    DisabledForUser = 2,
    DisabledByGroupPolicy = 3,
    DisabledByManifest = 4
}
