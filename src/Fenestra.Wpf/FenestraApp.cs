using System.Windows;
using System.Windows.Threading;
using Fenestra.Core;
using Fenestra.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

    // IHost
    public IServiceProvider Services => _host!.Services;

    public Task StartAsync(CancellationToken cancellationToken = default)
        => _host!.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
        => _host!.StopAsync(cancellationToken);

    public void Dispose()
    {
        _host?.Dispose();
        _cts.Dispose();
    }

    // IWpfApplication
    public AppInfo AppInfo { get; private set; } = null!;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = new FenestraBuilder();
        builder.SetArgs(e.Args);
        Configure(builder);

        builder.SetWpfAppInstance(this);
        _host = builder.BuildHost(shellType: null);
        AppInfo = _host.Services.GetRequiredService<AppInfo>();

        RegisterExceptionHandlers();
        StartAsync().GetAwaiter().GetResult();

        var mainWindow = ResolveMainWindow();
        if (mainWindow is null)
        {
            _cts.Cancel();
            StopAsync().GetAwaiter().GetResult();
            Shutdown(0);
            return;
        }

        mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        MainWindow = mainWindow;

        var minimizeToTray = Services.GetService(typeof(Services.MinimizeToTrayService)) as Services.MinimizeToTrayService;
        if (minimizeToTray != null && mainWindow is Core.IMinimizeToTray)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            minimizeToTray.Attach(mainWindow);
        }

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
