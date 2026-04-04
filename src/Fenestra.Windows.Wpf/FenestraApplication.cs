using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace Fenestra.Wpf;

/// <summary>
/// Wrapper host for WPF applications. Used with the Program.cs startup style.
/// Does not extend Application — creates or attaches to the consumer's Application instance.
/// </summary>
public class FenestraApplication : IHost, IWpfApplication
{
    private readonly IHost _host;
    private readonly Type? _appType;
    private readonly Type? _shellType;
    private readonly CancellationTokenSource _cts = new();

    internal FenestraApplication(IHost host, AppInfo appInfo, Type? appType, Type? shellType)
    {
        _host = host;
        AppInfo = appInfo;
        _appType = appType;
        _shellType = shellType;
    }

    /// <summary>
    /// Gets the dependency injection service provider for this application.
    /// </summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Starts the hosted services.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
        => _host.StartAsync(cancellationToken);

    /// <summary>
    /// Stops the hosted services.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default)
        => _host.StopAsync(cancellationToken);

    /// <summary>
    /// Disposes the underlying host and cancellation token source.
    /// </summary>
    public void Dispose()
    {
        _host.Dispose();
        _cts.Dispose();
    }

    /// <summary>
    /// Gets the application metadata (name, version, machine).
    /// </summary>
    public AppInfo AppInfo { get; }

    /// <summary>
    /// Gets a cancellation token that is signaled when the application shuts down.
    /// </summary>
    public CancellationToken ApplicationToken => _cts.Token;

    /// <summary>
    /// Shuts down the WPF application with the specified exit code.
    /// </summary>
    public void Shutdown(int exitCode = 0)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            try { _cts.Cancel(); } catch { }
            Application.Current.Shutdown(exitCode);
        });
    }

    /// <summary>
    /// Creates a builder.
    /// </summary>
    public static FenestraBuilder CreateBuilder()
    {
        return new FenestraBuilder();
    }

    /// <summary>
    /// Creates a builder with startup args.
    /// </summary>
    public static FenestraBuilder CreateBuilder(string[] args)
    {
        var builder = new FenestraBuilder();
        builder.SetArgs(args);
        return builder;
    }

    /// <summary>
    /// Creates a builder with a custom Application type and shell window.
    /// Consumer's .csproj must set EnableDefaultApplicationDefinition to false.
    /// </summary>
    public static FenestraBuilder CreateBuilder<TApp, TShell>()
        where TApp : Application, new()
        where TShell : Window
    {
        return new FenestraBuilder { AppType = typeof(TApp), ShellType = typeof(TShell) };
    }

    /// <summary>
    /// Creates a builder with a custom Application type, shell window, and startup args.
    /// </summary>
    public static FenestraBuilder CreateBuilder<TApp, TShell>(string[] args)
        where TApp : Application, new()
        where TShell : Window
    {
        var builder = new FenestraBuilder { AppType = typeof(TApp), ShellType = typeof(TShell) };
        builder.SetArgs(args);
        return builder;
    }

    /// <summary>
    /// Creates a builder with a shell window and default Application.
    /// </summary>
    public static FenestraBuilder CreateBuilder<TShell>()
        where TShell : Window
    {
        return new FenestraBuilder { ShellType = typeof(TShell) };
    }

    /// <summary>
    /// Creates a builder with a shell window and startup args.
    /// </summary>
    public static FenestraBuilder CreateBuilder<TShell>(string[] args)
        where TShell : Window
    {
        var builder = new FenestraBuilder { ShellType = typeof(TShell) };
        builder.SetArgs(args);
        return builder;
    }

    /// <summary>
    /// Starts the host and runs the WPF application message loop.
    /// Automatically ensures execution on an STA thread.
    /// </summary>
    public void Run()
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            Exception? caught = null;
            var thread = new Thread(() =>
            {
                try { RunCore(); }
                catch (Exception ex) { caught = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (caught is not null)
                throw caught;

            return;
        }

        RunCore();
    }

    private void RunCore()
    {
        var singleInstance = Services.GetService(typeof(Services.SingleInstanceGuard)) as Services.SingleInstanceGuard;
        if (singleInstance != null && !singleInstance.IsFirstInstance)
        {
            singleInstance.SendArguments(Environment.GetCommandLineArgs().Skip(1).ToArray());
            singleInstance.Dispose();
            Dispose();
            return;
        }

        var wpfApp = CreateWpfApplication();

        RegisterExceptionHandlers(wpfApp);

        StartAsync().GetAwaiter().GetResult();

        singleInstance?.StartListening();

        var toastActivation = Services.GetService(typeof(Windows.IToastActivationRegistrar)) as Windows.IToastActivationRegistrar;
        toastActivation?.Register();

        var shell = ResolveMainWindow();

        if (shell is null)
        {
            _cts.Cancel();
            StopAsync().GetAwaiter().GetResult();
            Dispose();
            return;
        }

        shell.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        wpfApp.MainWindow = shell;
        // When MinimizeToTray is active, use OnExplicitShutdown so hiding the main window
        // doesn't terminate the app. Otherwise use OnMainWindowClose.
        var minimizeToTray = Services.GetService(typeof(Services.MinimizeToTrayService)) as Services.MinimizeToTrayService;
        if (minimizeToTray != null && shell is Core.IMinimizeToTray)
        {
            wpfApp.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            minimizeToTray.Attach(shell);
        }
        else
        {
            wpfApp.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        wpfApp.Run(shell);

        var windowState = Services.GetService<Services.WindowStateService>();
        windowState?.SaveAll();
        _cts.Cancel();
        StopAsync().GetAwaiter().GetResult();
        Dispose();
    }

    private Window? ResolveMainWindow()
    {
        var factory = Services.GetService<IMainWindowFactory>();
        if (factory is not null)
        {
            return factory.Create();
        }

        if (_shellType is null)
        {
            throw new InvalidOperationException(
                "No IMainWindowFactory registered and no main window type specified. " +
                "Use UseMainWindow<T>() or UseMainWindowFactory<T>().");
        }

        return (Window)Services.GetRequiredService(_shellType);
    }

    private Application CreateWpfApplication()
    {
        if (Application.Current != null)
            return Application.Current;

        if (_appType != null)
        {
            var app = (Application)Activator.CreateInstance(_appType)!;

            var initMethod = _appType.GetMethod(
                "InitializeComponent",
                BindingFlags.Public | BindingFlags.Instance,
                null, Type.EmptyTypes, null);

            initMethod?.Invoke(app, null);

            return app;
        }

        return new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
    }

    private void RegisterExceptionHandlers(Application application)
    {
        application.DispatcherUnhandledException += OnDispatcherUnhandledException;
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
