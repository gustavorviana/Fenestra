namespace Fenestra.Windows;

/// <summary>
/// Provides access to the taskbar progress indicator.
/// Only one active progress instance is allowed per window.
/// </summary>
public interface ITaskbarProvider
{
    /// <summary>
    /// Creates a taskbar progress indicator for the specified window.
    /// </summary>
    ITaskbarProgress Create(object window);
}
