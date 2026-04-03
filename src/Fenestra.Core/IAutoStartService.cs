using Fenestra.Core.Models;

namespace Fenestra.Core;

/// <summary>
/// Manages Windows startup registration for the application.
/// </summary>
public interface IAutoStartService
{
    /// <summary>
    /// Gets whether the application is currently registered for startup.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Checks whether the startup entry has been initialized with the specified arguments.
    /// </summary>
    bool IsInitialized(params string[] args);

    /// <summary>
    /// Registers the application to start automatically with the specified arguments.
    /// </summary>
    void Enable(params string[] args);

    /// <summary>
    /// Removes the application from automatic startup.
    /// </summary>
    void Disable();

    /// <summary>
    /// Returns the current startup approval status, or null if no entry exists.
    /// </summary>
    StartupStatus? GetStatus();
}
