using Fenestra.Core.Models;

namespace Fenestra.Core;

/// <summary>
/// Controls the taskbar progress indicator for a specific window. Dispose to clear progress.
/// </summary>
public interface ITaskbarProgress : IDisposable
{
    /// <summary>
    /// Sets the progress bar value (0.0 to 1.0) on the taskbar button.
    /// </summary>
    void SetProgress(double value);

    /// <summary>
    /// Sets the progress indicator state (normal, paused, error, or indeterminate) on the taskbar button.
    /// </summary>
    void SetState(TaskbarProgressState state);
}
