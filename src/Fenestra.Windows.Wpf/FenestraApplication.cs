using Fenestra.Core.Models;
using Fenestra.Windows;
using Fenestra.Wpf.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Windows;

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
    public static WpfFenestraBuilder CreateBuilder()
    {
        return new WpfFenestraBuilder();
    }

    /// <summary>
    /// Creates a builder with startup args.
    /// </summary>
    public static WpfFenestraBuilder CreateBuilder(string[] args)
    {
        var builder = new WpfFenestraBuilder();
        builder.SetArgs(args);
        return builder;
    }

    /// <summary>
    /// Creates a builder with a custom Application type and shell window.
    /// Consumer's .csproj must set EnableDefaultApplicationDefinition to false.
    /// </summary>
    public static WpfFenestraBuilder CreateBuilder<TApp, TShell>()
        where TApp : Application, new()
        where TShell : Window
    {
        return new WpfFenestraBuilder { AppType = typeof(TApp), ShellType = typeof(TShell) };
    }

    /// <summary>
    /// Creates a builder with a custom Application type, shell window, and startup args.
    /// </summary>
    public static WpfFenestraBuilder CreateBuilder<TApp, TShell>(string[] args)
        where TApp : Application, new()
        where TShell : Window
    {
        var builder = new WpfFenestraBuilder { AppType = typeof(TApp), ShellType = typeof(TShell) };
        builder.SetArgs(args);
        return builder;
    }

    /// <summary>
    /// Creates a builder with a shell window and default Application.
    /// </summary>
    public static WpfFenestraBuilder CreateBuilder<TShell>()
        where TShell : Window
    {
        return new WpfFenestraBuilder { ShellType = typeof(TShell) };
    }

    /// <summary>
    /// Creates a builder with a shell window and startup args.
    /// </summary>
    public static WpfFenestraBuilder CreateBuilder<TShell>(string[] args)
        where TShell : Window
    {
        var builder = new WpfFenestraBuilder { ShellType = typeof(TShell) };
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
                try { RunCoreAsync().GetAwaiter().GetResult(); }
                catch (Exception ex) { caught = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (caught is not null)
                throw caught;

            return;
        }

        RunCoreAsync().GetAwaiter().GetResult();
    }

    private async Task RunCoreAsync()
    {
        var wpfApp = CreateWpfApplication();
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();

        var status = await FenestraStartupPipeline.RunAsync(
            Services, _host, _cts, args, wpfApp);

        if (status != StartupStatus.Continue)
        {
            Dispose();
            return;
        }

        var shell = ResolveMainWindow();
        if (shell is null)
        {
            _cts.Cancel();
            await StopAsync();
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
        await StopAsync();
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
}
