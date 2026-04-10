namespace Fenestra.Core;

/// <summary>
/// Contract for splash screens displayed during the Fenestra startup pipeline.
/// A splash owns its full lifecycle: it shows itself, runs its loading logic, and closes itself.
/// The pipeline awaits <see cref="RunAsync"/> before creating or showing the main window, so no
/// other window can open while the splash is active.
/// </summary>
/// <remarks>
/// Implementations are typically resolved from the DI container (after the host has started),
/// so they may receive services via constructor injection. The splash's loading logic is
/// responsible for whatever "app-level" initialization must complete before the user can interact
/// with the main window.
/// </remarks>
public interface ISplashScreen
{
    /// <summary>
    /// Runs the splash end to end. The implementation MUST:
    /// <list type="number">
    ///   <item><description>Show the splash window / visual.</description></item>
    ///   <item><description>Execute its loading logic, reporting updates via <paramref name="progress"/>.</description></item>
    ///   <item><description>Close the splash.</description></item>
    /// </list>
    /// The returned <see cref="Task"/> completes only after the splash has been closed — this is
    /// the signal the pipeline uses to release control and show the main window.
    /// </summary>
    /// <param name="progress">
    /// Progress reporter supplied by the pipeline. The splash receives this at show/create time
    /// and forwards its status updates through it so external observers (logging, telemetry,
    /// tests) can see the same messages the user sees.
    /// </param>
    /// <param name="cancellationToken">
    /// Token signaled when the application is shutting down. Implementations should honor
    /// cancellation by closing the splash and returning as quickly as possible.
    /// </param>
    Task RunAsync(IProgress<SplashStatus> progress, CancellationToken cancellationToken);
}
