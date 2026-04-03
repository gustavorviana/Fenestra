using Fenestra.Core.Models;

namespace Fenestra.Core;

/// <summary>
/// Controls the taskbar progress indicator for the main window.
/// </summary>
public interface ITaskbarService
{
    void SetProgress(double value);
    void SetProgressState(TaskbarProgressState state);
    void ClearProgress();
}
