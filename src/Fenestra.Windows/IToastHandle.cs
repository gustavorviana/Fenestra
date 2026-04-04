using Fenestra.Windows.Models;

namespace Fenestra.Windows;

/// <summary>
/// Handle to an active toast notification. Allows updating, hiding, removing, and listening to events.
/// </summary>
public interface IToastHandle : IDisposable
{
    /// <summary>
    /// Gets the tag identifying this toast.
    /// </summary>
    string Tag { get; }

    /// <summary>
    /// Gets the group this toast belongs to.
    /// </summary>
    string? Group { get; }

    /// <summary>
    /// Gets the current lifecycle state of this toast.
    /// </summary>
    ToastHandleState State { get; }

    /// <summary>
    /// Gets whether the popup was suppressed when this toast was shown.
    /// When true, the toast goes directly to Action Center without appearing on screen.
    /// </summary>
    bool SuppressPopup { get; }

    /// <summary>
    /// Gets the priority of this toast notification.
    /// </summary>
    ToastPriority Priority { get; }

    /// <summary>
    /// Gets whether this toast is automatically removed from Action Center when the device reboots.
    /// </summary>
    bool ExpiresOnReboot { get; }

    /// <summary>
    /// Gets the absolute time when this toast auto-expires from Action Center.
    /// Null means the notification uses the system default expiration (3 days).
    /// </summary>
    DateTimeOffset? ExpirationTime { get; }

    /// <summary>
    /// Updates the data bindings of this toast (for progress bars).
    /// </summary>
    void Update(Dictionary<string, string> data);

    /// <summary>
    /// Replaces the toast content by rebuilding it via the builder (keeps the same tag/group).
    /// </summary>
    void Update(Action<ToastBuilder> configure);

    /// <summary>
    /// Dismisses this toast immediately from the screen and removes it from Action Center.
    /// Uses IToastNotifier.Hide which removes the notification entirely.
    /// </summary>
    void Hide();

    /// <summary>
    /// Removes this toast from Action Center by tag (and group, if set).
    /// The toast may still be visible on screen until it times out or the user dismisses it.
    /// </summary>
    void Remove();

    /// <summary>
    /// Removes all notifications in this toast's group from Action Center.
    /// Requires <see cref="Group"/> to be set; does nothing if Group is null.
    /// </summary>
    void RemoveGroup();

    /// <summary>
    /// Raised when the user clicks a button or the toast body.
    /// </summary>
    event EventHandler<ToastActivatedArgs>? Activated;

    /// <summary>
    /// Raised when this toast is dismissed (by user, timeout, or application).
    /// </summary>
    event EventHandler<ToastDismissalReason>? Dismissed;

    /// <summary>
    /// Raised when this toast fails to display.
    /// </summary>
    event EventHandler<int>? Failed;
}
