using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Wpf.Extensions;
using Fenestra.Wpf.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Windows;

namespace Fenestra.Wpf;

public class FenestraBuilder
{
    private AppInfo? _appInfo;
    private IWpfApplication? _wpfAppInstance;
    private string[]? _args;
    private readonly List<Action<ILoggingBuilder>> _loggingActions = new();
    private readonly List<Action<IServiceCollection>> _serviceActions = new();

    internal FenestraBuilder()
    {
    }

    /// <summary>
    /// The consumer's Application type (set by CreateBuilder). Null means default Application.
    /// </summary>
    internal Type? AppType { get; set; }

    /// <summary>
    /// The main window type. Used by FenestraApplication.Build().
    /// </summary>
    internal Type? ShellType { get; set; }

    /// <summary>
    /// Service collection for the primary host.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <summary>
    /// Configuration for the primary host.
    /// </summary>
    public ConfigurationManager Configuration { get; } = new();

    /// <summary>
    /// The current environment name (e.g., Development, Production).
    /// Defaults to DOTNET_ENVIRONMENT or ASPNETCORE_ENVIRONMENT, or "Production" if not set.
    /// </summary>
    public string Environment { get; set; } =
        System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? "Production";

    /// <summary>
    /// Sets the application name. Version is resolved from the entry assembly.
    /// </summary>
    public FenestraBuilder UseAppName(string appName)
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var version = assembly.GetName().Version ?? new Version(1, 0, 0);
        _appInfo = new AppInfo(appName, version, System.Environment.MachineName);
        return this;
    }

    /// <summary>
    /// Defines application info explicitly.
    /// </summary>
    public FenestraBuilder UseAppInfo(string appName, Version version)
    {
        _appInfo = new AppInfo(appName, version, System.Environment.MachineName);
        return this;
    }

    /// <summary>
    /// Registers all Window types from the assembly containing TMarker.
    /// </summary>
    public FenestraBuilder RegisterWindows<TMarker>()
    {
        Services.RegisterWindows<TMarker>();
        return this;
    }

    /// <summary>
    /// Registers all Window types from the assemblies containing the specified marker types.
    /// </summary>
    public FenestraBuilder RegisterWindows(params Type[] markers)
    {
        Services.RegisterWindows(markers);
        return this;
    }

    /// <summary>
    /// Registers all Window types via assembly scan.
    /// </summary>
    public FenestraBuilder RegisterWindows()
    {
        Services.RegisterWindows();
        return this;
    }

    /// <summary>
    /// Sets the main window type for the application.
    /// </summary>
    public FenestraBuilder UseMainWindow<TShell>() where TShell : Window
    {
        ShellType = typeof(TShell);
        return this;
    }

    /// <summary>
    /// Registers a factory that dynamically creates the main window.
    /// The factory can return null to shut down the application without UI.
    /// </summary>
    public FenestraBuilder UseMainWindowFactory<TFactory>() where TFactory : class, IMainWindowFactory
    {
        Services.AddSingleton<IMainWindowFactory, TFactory>();
        return this;
    }

    /// <summary>
    /// Enables the system tray icon service.
    /// </summary>
    public FenestraBuilder UseTrayIcon()
    {
        Services.AddSingleton<ITrayIconService, TrayIconService>();
        return this;
    }

    /// <summary>
    /// Enables minimize-to-tray behavior for windows that implement IMinimizeToTray.
    /// Implies UseTrayIcon().
    /// </summary>
    public FenestraBuilder UseMinimizeToTray(Action<MinimizeToTrayOptions>? configure = null)
    {
        UseTrayIcon();
        var options = new MinimizeToTrayOptions();
        configure?.Invoke(options);
        Services.AddSingleton(options);
        Services.AddSingleton<MinimizeToTrayService>();
        return this;
    }

    /// <summary>
    /// Configures logging for the application.
    /// </summary>
    public FenestraBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        _loggingActions.Add(configure);
        return this;
    }

    /// <summary>
    /// Configures additional services.
    /// </summary>
    public FenestraBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        _serviceActions.Add(configure);
        return this;
    }

    /// <summary>
    /// Builds the FenestraApplication.
    /// </summary>
    public FenestraApplication Build()
    {
        var host = BuildHost(ShellType);
        var appInfo = _appInfo ?? ResolveAppInfoFromAssembly();
        var app = new FenestraApplication(host, appInfo, AppType, ShellType);
        _wpfAppInstance = app;
        return app;
    }

    /// <summary>
    /// Builds the primary host.
    /// Used internally by both FenestraApplication and FenestraApp.
    /// </summary>
    internal IHost BuildHost(Type? shellType)
    {
        var appInfo = _appInfo ?? ResolveAppInfoFromAssembly();

        var primaryHostBuilder = Host.CreateDefaultBuilder();

        primaryHostBuilder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddJsonFile($"appsettings.{Environment}.json", optional: true, reloadOnChange: true);
            config.AddEnvironmentVariables();

            foreach (var source in ((IConfigurationBuilder)Configuration).Sources)
            {
                config.Add(source);
            }

            if (_args is { Length: > 0 })
            {
                config.AddCommandLine(_args);
            }
        });

        primaryHostBuilder.ConfigureServices((_, services) =>
        {
            foreach (var descriptor in Services)
            {
                services.Add(descriptor);
            }

            services.AddSingleton(appInfo);
            services.AddSingleton<WindowStateService>();
            services.AddSingleton<IWindowManager, WindowManager>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<ITaskbarService, TaskbarService>();
            services.TryAddSingleton<IExceptionHandler, DefaultExceptionHandler>();
            services.AddSingleton<IWpfApplication>(_ => _wpfAppInstance!);

            if (shellType != null)
            {
                services.AddTransient(shellType);
            }
        });

        primaryHostBuilder.ConfigureServices((_, services) =>
        {
            foreach (var action in _serviceActions)
            {
                action(services);
            }
        });

        primaryHostBuilder.ConfigureLogging(logging =>
        {
            logging.AddDebug();

            foreach (var action in _loggingActions)
            {
                action(logging);
            }
        });

        return primaryHostBuilder.Build();
    }

    internal void SetWpfAppInstance(IWpfApplication instance) => _wpfAppInstance = instance;

    internal void SetArgs(string[] args) => _args = args;

    private static AppInfo ResolveAppInfoFromAssembly()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var name = assembly.GetName();
        return new AppInfo(
            name.Name ?? "FenestraApp",
            name.Version ?? new Version(1, 0, 0),
            System.Environment.MachineName);
    }
}
