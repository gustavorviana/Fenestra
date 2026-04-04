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
}
