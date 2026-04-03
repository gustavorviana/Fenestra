using Fenestra.Core.Models;

namespace Fenestra.Core;

/// <summary>
/// Provides access to the running WPF application host and its services.
/// </summary>
public interface IWpfApplication
{
    /// <summary>
    /// Gets the application metadata.
    /// </summary>
    AppInfo AppInfo { get; }

    /// <summary>
    /// Gets the dependency injection service provider.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets a cancellation token that is cancelled when the application is shutting down.
    /// </summary>
    CancellationToken ApplicationToken { get; }

    /// <summary>
    /// Initiates an orderly shutdown of the application with the specified exit code.
    /// </summary>
    void Shutdown(int exitCode = 0);
}
