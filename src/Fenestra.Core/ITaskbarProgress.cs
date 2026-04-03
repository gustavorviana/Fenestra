using Fenestra.Core.Models;

namespace Fenestra.Core;

public interface ITaskbarProgress : IDisposable
{
    void SetProgress(double value);
    void SetState(TaskbarProgressState state);
}
