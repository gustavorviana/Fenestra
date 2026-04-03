namespace Fenestra.Core;

/// <summary>
/// Provides access to the taskbar progress indicator.
/// Only one active progress instance is allowed per window.
/// </summary>
public interface ITaskbarProvider
{
    ITaskbarProgress Create(object window);
}
