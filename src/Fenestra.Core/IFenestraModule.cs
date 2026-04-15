namespace Fenestra.Core;

/// <summary>
/// Contract for framework modules that require asynchronous initialization during application
/// startup. Modules are resolved from the DI container and <see cref="InitAsync"/> is awaited
/// on each one before the host finishes starting and before any window -- splash or main --
/// is shown.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are initialized in DI registration order. An exception thrown from
/// <see cref="InitAsync"/> propagates out of the startup sequence and aborts application launch.
/// </para>
/// <para>
/// Use this interface for cross-cutting work that must complete before the UI appears:
/// applying a persisted culture, warming caches, acquiring a license, downloading an initial
/// payload, etc. Implementations are typically also registered under another interface
/// (their "functional" interface) via a factory lambda so the same singleton is resolvable
/// both ways.
/// </para>
/// </remarks>
public interface IFenestraModule
{
    /// <summary>
    /// Runs the module's one-time initialization. Called exactly once per application lifetime.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token signaled when the application is shutting down. Implementations should honor
    /// cancellation and return as quickly as possible.
    /// </param>
    Task InitAsync(CancellationToken cancellationToken);
}
