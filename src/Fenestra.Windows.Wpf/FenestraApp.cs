using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System.Windows.Threading;

namespace Fenestra.Wpf;

/// <summary>
/// Base class for WPF applications using the App.xaml.cs startup style.
/// Extends Application and manages the hosting lifecycle via OnStartup/OnExit.
///
/// Usage:
/// <code>
/// public partial class App : FenestraApp
/// {
///     protected override void Configure(FenestraBuilder builder)
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
    protected abstract void Configure(FenestraBuilder builder);

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

        var builder = new FenestraBuilder();
        builder.SetArgs(e.Args);
        Configure(builder);

        builder.SetWpfAppInstance(this);
        _host = builder.BuildHost(shellType: null);
        AppInfo = _host.Services.GetRequiredService<AppInfo>();

        var singleInstance = Services.GetService<Services.SingleInstanceGuard>();
        if (singleInstance != null && !singleInstance.IsFirstInstance)
        {
            singleInstance.SendArguments(e.Args);
            singleInstance.Dispose();
            Shutdown(0);
            return;
        }

        RegisterExceptionHandlers();
        StartAsync().GetAwaiter().GetResult();

        singleInstance?.StartListening();

        var mainWindow = ResolveMainWindow();
        if (mainWindow is null)
        {
            _cts.Cancel();
            StopAsync().GetAwaiter().GetResult();
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
        _cts.Cancel();
        StopAsync().GetAwaiter().GetResult();
        Dispose();
        base.OnExit(e);
    }

    private void RegisterExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var context = new FenestraExceptionContext(e.Exception, isCritical: false);
        HandleException(context);
        e.Handled = context.Handled;
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            var context = new FenestraExceptionContext(ex, isCritical: true);
            HandleException(context);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var context = new FenestraExceptionContext(e.Exception, isCritical: false);
        HandleException(context);
        if (context.Handled)
        {
            e.SetObserved();
        }
    }

    private void HandleException(FenestraExceptionContext context)
    {
        var handler = Services.GetRequiredService<IExceptionHandler>();
        handler.Handle(context);
    }
}
