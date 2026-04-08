using Fenestra.Windows.Models;

namespace Fenestra.Windows;

/// <summary>
/// Displays and manages Windows toast notifications.
/// </summary>
public interface IToastService
{
    /// <summary>
    /// Shows a toast notification and returns a handle for further interaction.
    /// </summary>
    IToastHandle Show(ToastContent toast);

    /// <summary>
    /// Shows a toast notification configured via the fluent builder and returns a handle.
    /// </summary>
    IToastHandle Show(Action<ToastBuilder> configure);

    /// <summary>
    /// Gets all active (not yet dismissed/removed) toast handles.
    /// </summary>
    IReadOnlyList<IToastHandle> Active { get; }

    /// <summary>
    /// Clears all toasts from Action Center for this application.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Removes all toasts in the specified group from Action Center.
    /// </summary>
    void ClearHistory(string group);

    /// <summary>
    /// Removes a specific toast (by tag and group) from Action Center.
    /// </summary>
    void ClearHistory(string tag, string group);

    /// <summary>
    /// Queries whether notifications are enabled for this application.
    /// </summary>
    NotificationSetting GetSetting();

    /// <summary>
    /// Finds an active toast handle by its tag. Returns null if not found.
    /// </summary>
    IToastHandle? FindByTag(string tag);

    /// <summary>
    /// Finds all active toast handles in the specified group.
    /// </summary>
    IReadOnlyList<IToastHandle> FindByGroup(string group);

    /// <summary>
    /// Schedules a toast notification for future delivery.
    /// </summary>
    IScheduledToastHandle Schedule(ToastContent toast, DateTimeOffset deliveryTime);

    /// <summary>
    /// Schedules a toast notification configured via the fluent builder for future delivery.
    /// </summary>
    IScheduledToastHandle Schedule(Action<ToastBuilder> configure, DateTimeOffset deliveryTime);

    /// <summary>
    /// Gets all currently scheduled toast notifications.
    /// </summary>
    IReadOnlyList<IScheduledToastHandle> Scheduled { get; }
}
