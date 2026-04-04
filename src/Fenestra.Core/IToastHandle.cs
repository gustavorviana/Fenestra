using Fenestra.Core.Models;

namespace Fenestra.Core;

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
    /// Updates the data bindings of this toast (for progress bars).
    /// </summary>
    void Update(Dictionary<string, string> data);

    /// <summary>
    /// Replaces the toast content by rebuilding it via the builder (keeps the same tag/group).
    /// </summary>
    void Update(Action<ToastBuilder> configure);

    /// <summary>
    /// Hides this toast from the screen.
    /// </summary>
    void Hide();

    /// <summary>
    /// Removes this toast from Action Center.
    /// </summary>
    void Remove();

    /// <summary>
    /// Raised when the user clicks a button or the toast body.
    /// </summary>
    event EventHandler<ToastActivatedArgs>? Activated;

    /// <summary>
    /// Raised when this toast is dismissed.
    /// </summary>
    event EventHandler<ToastDismissalReason>? Dismissed;

    /// <summary>
    /// Raised when this toast fails to display.
    /// </summary>
    event EventHandler<int>? Failed;
}
