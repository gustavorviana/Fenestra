using Fenestra.Core.Models;
using Fenestra.Windows;
using Fenestra.Wpf.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Fenestra.Wpf;

/// <summary>
/// Base class for WPF applications using the App.xaml.cs startup style.
/// Extends Application and manages the hosting lifecycle via OnStartup/OnExit.
///
/// Usage:
/// <code>
/// public partial class App : FenestraApp
/// {
///     protected override void Configure(WpfFenestraBuilder builder)
///     {
///         builder.Services.AddSingleton&lt;IMyService, MyService&gt;();
///         builder.RegisterWindows();
///     }
///
///     protected override Window CreateMainWindow(IServiceProvider services)
///     {
///         return services.GetRequiredService&lt;MainWindow&gt;();
///     }
/// }
/// </code>
/// </summary>
public abstract class FenestraApp : Application, IHost, IWpfApplication
{
    private IHost? _host;
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Gets the dependency injection service provider for this application.
    /// </summary>
    public IServiceProvider Services => _host!.Services;

    /// <summary>
    /// Starts the hosted services.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
        => _host!.StartAsync(cancellationToken);

    /// <summary>
    /// Stops the hosted services.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default)
        => _host!.StopAsync(cancellationToken);

    /// <summary>
    /// Disposes the underlying host and cancellation token source.
    /// </summary>
    public void Dispose()
    {
        _host?.Dispose();
        _cts.Dispose();
    }

    /// <summary>
    /// Gets the application metadata (name, version, machine).
    /// </summary>
    public AppInfo AppInfo { get; private set; } = null!;

    /// <summary>
    /// Gets a cancellation token that is signaled when the application shuts down.
    /// </summary>
    public CancellationToken ApplicationToken => _cts.Token;

    void IWpfApplication.Shutdown(int exitCode)
    {
        _cts.Cancel();
        Dispatcher.Invoke(() => Shutdown(exitCode));
    }

    /// <summary>
    /// Configure services, logging, and additional hosts.
    /// </summary>
    protected abstract void Configure(WpfFenestraBuilder builder);

    /// <summary>
    /// Create the main shell window from the DI container.
    /// </summary>
    protected abstract Window CreateMainWindow(IServiceProvider services);

    /// <summary>
    /// Called after all services are initialized and the main window is created, but before it is shown.
    /// Use this to configure tray menus, hotkeys, event subscriptions, etc.
    /// </summary>
    protected virtual void OnReady(IServiceProvider services, Window mainWindow)
    {
    }

    /// <summary>
    /// Initializes the host, registers services, and shows the main window on startup.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        InitAsync(e).GetAwaiter().GetResult();
    }

    private async Task InitAsync(StartupEventArgs e)
    {
        var builder = new WpfFenestraBuilder();
        builder.SetArgs(e.Args);
        Configure(builder);

        builder.SetWpfAppInstance(this);
        _host = builder.BuildHostInternal();
        AppInfo = _host.Services.GetRequiredService<AppInfo>();

        var status = await FenestraStartupPipeline.RunAsync(
            Services, _host, _cts, e.Args, this);

        if (status != StartupStatus.Continue)
        {
            Shutdown(0);
            return;
        }

        var mainWindow = ResolveMainWindow();
        if (mainWindow is null)
        {
            _cts.Cancel();
            await StopAsync();
            Shutdown(0);
            return;
        }

        mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var windowState = Services.GetRequiredService<Services.WindowStateService>();
        windowState.Attach(mainWindow);

        MainWindow = mainWindow;

        var minimizeToTray = Services.GetService(typeof(Services.MinimizeToTrayService)) as Services.MinimizeToTrayService;
        if (minimizeToTray != null && mainWindow is Core.IMinimizeToTray)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            minimizeToTray.Attach(mainWindow);
        }

        OnReady(Services, mainWindow);

        mainWindow.Show();
    }

    private Window? ResolveMainWindow()
    {
        var factory = Services.GetService<IMainWindowFactory>();
        if (factory is not null)
        {
            return factory.Create();
        }

        return CreateMainWindow(Services);
    }

    /// <summary>
    /// Stops the host and disposes resources when the application exits.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        var windowState = Services.GetService<Services.WindowStateService>();
        windowState?.SaveAll();

        _cts.Cancel();
        StopAsync().GetAwaiter().GetResult();
        Dispose();
        base.OnExit(e);
    }
}
