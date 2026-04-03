using Fenestra.Core.Models;

namespace Fenestra.Core;

public interface IWpfApplication
{
    AppInfo AppInfo { get; }
    IServiceProvider Services { get; }
    CancellationToken ApplicationToken { get; }
    void Shutdown(int exitCode = 0);
}
