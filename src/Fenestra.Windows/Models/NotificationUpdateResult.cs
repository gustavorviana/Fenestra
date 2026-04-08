namespace Fenestra.Windows.Models;

/// <summary>
/// Specifies the result of a toast notification data update.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.notificationupdateresult
/// </remarks>
public enum NotificationUpdateResult
{
    Succeeded = 0,
    Failed = 1,
    NotificationNotFound = 2
}
