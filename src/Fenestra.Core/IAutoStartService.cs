using Fenestra.Core.Models;

namespace Fenestra.Core;

public interface IAutoStartService
{
    bool IsEnabled { get; }
    bool IsInitialized(params string[] args);
    void Enable(params string[] args);
    void Disable();
    StartupStatus? GetStatus();
}
