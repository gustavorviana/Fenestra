using Fenestra.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Fenestra.Wpf.Services;

/// <summary>
/// Outcome of <see cref="FenestraStartupPipeline.RunAsync"/>. Tells the caller whether
/// to proceed to main-window resolution or to tear the app down.
/// </summary>
internal enum StartupStatus
{
    /// <summary>Init completed; the caller should proceed to main-window resolution.</summary>
    Continue,

    /// <summary>Another instance is already running; the caller should tear down without showing UI.</summary>
    SecondInstance,

    /// <summary>
    /// The user cancelled the splash before loading completed. The pipeline has already
    /// cancelled the <see cref="CancellationTokenSource"/> and called <see cref="IHost.StopAsync"/>;
    /// the caller only needs to perform its platform-specific teardown (<c>Shutdown</c> vs <c>Dispose</c>).
    /// </summary>
    SplashCancelled,
}

/// <summary>
/// Runs the portion of the Fenestra WPF startup pipeline that is identical between
/// the App-style (<see cref="FenestraApp"/>) and Builder-style (<see cref="FenestraApplication"/>)
/// entry points: single-instance enforcement, exception handler binding, framework module
/// initialization (<see cref="IFenestraModule.InitAsync"/>), host startup, and the splash
/// coordinator lifecycle.
/// </summary>
/// <remarks>
/// What the pipeline does NOT cover (and must stay with each caller because the two entry
/// points genuinely diverge):
/// <list type="bullet">
///   <item><description>How the <see cref="IHost"/> is built or acquired.</description></item>
///   <item><description>Main window resolution (abstract <c>CreateMainWindow</c> vs registered shell type).</description></item>
///   <item><description>Showing the main window (<see cref="Window.Show"/> vs <see cref="Application.Run(Window)"/>).</description></item>
///   <item><description>Platform-specific teardown on bail (<see cref="Application.Shutdown(int)"/> vs <see cref="IDisposable.Dispose"/>).</description></item>
/// </list>
/// </remarks>
internal static class FenestraStartupPipeline
{
    /// <summary>
    /// Runs the shared startup steps.
    /// </summary>
    /// <param name="services">The DI container.</param>
    /// <param name="host">The host whose <see cref="IHost.StartAsync"/> should be awaited.</param>
    /// <param name="cts">Application-level cancellation source, signaled on shutdown.</param>
    /// <param name="args">
    /// Command-line arguments. Forwarded to the first instance when the current process is a
    /// second instance. Sources differ between entry points (<c>StartupEventArgs.Args</c> for
    /// App-style, <see cref="Environment.GetCommandLineArgs"/> for Builder-style).
    /// </param>
    /// <param name="wpfApplication">
    /// The WPF <see cref="Application"/> used to bind exception handlers. For App-style this
    /// is <c>this</c>; for Builder-style it is the Application instance created (or adopted)
    /// by <c>FenestraApplication</c>.
    /// </param>
    public static async Task<StartupStatus> RunAsync(
        IServiceProvider services,
        IHost host,
        CancellationTokenSource cts,
        string[] args,
        Application wpfApplication)
    {
        // Single-instance enforcement. When another instance is already running, forward the
        // args through the named pipe and bail; the caller performs its platform-specific teardown.
        var singleInstance = services.GetService<SingleInstanceGuard>();
        if (singleInstance != null && !singleInstance.IsFirstInstance)
        {
            singleInstance.SendArguments(args);
            singleInstance.Dispose();
            return StartupStatus.SecondInstance;
        }

        // Exception handler wiring is opt-in via UseErrorHandler(). Only install when an
        // IExceptionHandler is actually registered — otherwise the callbacks would throw
        // at runtime trying to resolve a missing service.
        if (services.GetService<IExceptionHandler>() != null)
            new FenestraWpfExceptionHandling(services).Attach(wpfApplication);

        // Initialize framework modules (localization, single-instance pipe listener,
        // toast background activation, etc.) before any hosted service or UI materializes.
        // Modules run in DI registration order.
        foreach (var module in services.GetServices<IFenestraModule>())
            await module.InitAsync(cts.Token);

        await host.StartAsync();

        // Splash runs its full lifecycle (show → load → close) before any other window is
        // resolved, so nothing else can open while the splash is on screen. The splash owns
        // the loading logic (via ISplashScreen.RunAsync) and signals completion by returning
        // from RunToCompletion — only then do we proceed to the main window. If the user
        // cancels the splash (via the close button), RunToCompletion throws OCE and we tear
        // the host down without ever showing the main window.
        var splashCoordinator = services.GetService<WpfSplashCoordinator>();
        try
        {
            splashCoordinator?.RunToCompletion(cts.Token);
        }
        catch (OperationCanceledException)
        {
            cts.Cancel();
            await host.StopAsync();
            return StartupStatus.SplashCancelled;
        }

        return StartupStatus.Continue;
    }
}
