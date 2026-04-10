using Fenestra.Core;
using System.Windows;
using System.Windows.Threading;

namespace Fenestra.Wpf.Services;

/// <summary>
/// Orchestrates the splash screen during WPF application startup and serves as the
/// <see cref="IProgress{T}"/> channel consumed by the active splash. External services
/// (registered in DI as <see cref="IProgress{T}"/> of <see cref="SplashStatus"/>) and the
/// splash itself write to the same sink, and subscribers can hook <see cref="ProgressReported"/>
/// to observe the stream for logging, telemetry or tests.
/// </summary>
/// <remarks>
/// The coordinator is always registered, even when no splash is configured, so consumer
/// services can inject <see cref="IProgress{T}"/> unconditionally and report status without
/// caring whether a splash is on screen.
/// </remarks>
public sealed class WpfSplashCoordinator : IProgress<SplashStatus>
{
    private readonly Type? _splashScreenType;
    private readonly IServiceProvider _services;

    internal WpfSplashCoordinator(Type? splashScreenType, IServiceProvider services)
    {
        _splashScreenType = splashScreenType;
        _services = services;
    }

    /// <summary>Raised on every <see cref="IProgress{T}.Report"/> call.</summary>
    public event EventHandler<SplashStatus>? ProgressReported;

    /// <inheritdoc />
    public void Report(SplashStatus value)
        => ProgressReported?.Invoke(this, value);

    /// <summary>
    /// Runs the configured splash to completion on the current WPF dispatcher thread.
    /// Pumps a nested message loop via <see cref="Dispatcher.PushFrame"/> so the splash
    /// window can render and handle input while its async loading logic progresses.
    /// Returns only after the splash has been closed.
    /// </summary>
    /// <remarks>
    /// Must be called on the dispatcher (STA) thread. No-op when no splash is configured.
    /// Exceptions raised by the splash's <see cref="ISplashScreen.RunAsync"/> are rethrown
    /// on the calling thread after the nested message loop exits.
    /// </remarks>
    internal void RunToCompletion(CancellationToken cancellationToken)
    {
        var splash = _services.GetService(_splashScreenType!) as ISplashScreen
            ?? throw new InvalidOperationException(
                $"Splash type '{_splashScreenType!.FullName}' is not registered in DI " +
                $"or does not implement ISplashScreen. Ensure the type was registered via " +
                $"UseSplashScreen<T>() and implements ISplashScreen.");

        var dispatcher = Dispatcher.CurrentDispatcher;
        var frame = new DispatcherFrame();
        Exception? capturedException = null;

        // WPF auto-assigns the first Window shown to Application.MainWindow when MainWindow
        // is still null. If we left ShutdownMode at its default (OnMainWindowClose / OnLastWindowClose),
        // closing the splash would flip the app into shutdown state BEFORE the real main window
        // is ever created — any subsequent Show() would throw "the application has already
        // entered shutdown state". Force OnExplicitShutdown for the duration of the splash,
        // then restore the previous mode so the pipeline's later configuration still applies.
        var app = Application.Current;
        var previousShutdownMode = app?.ShutdownMode ?? ShutdownMode.OnLastWindowClose;
        var previousMainWindow = app?.MainWindow;
        if (app != null)
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            // InvokeAsync queues the lambda so RunAsync begins *after* PushFrame starts pumping.
            // This guarantees a dispatcher tick is available for the splash's first render even
            // if LoadAsync is implemented synchronously.
            dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await splash.RunAsync(this, cancellationToken);
                }
                catch (Exception ex)
                {
                    capturedException = ex;
                }
                finally
                {
                    frame.Continue = false;
                }
            });

            Dispatcher.PushFrame(frame);
        }
        finally
        {
            if (app != null)
            {
                // If the splash window hijacked MainWindow, clear it so the pipeline can assign
                // the real shell. Leave any pre-existing MainWindow alone.
                if (!ReferenceEquals(app.MainWindow, previousMainWindow))
                    app.MainWindow = previousMainWindow!;

                app.ShutdownMode = previousShutdownMode;
            }
        }

        if (capturedException != null)
            throw capturedException;
    }
}
