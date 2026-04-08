using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows;
using Fenestra.Wpf.Extensions;
using Fenestra.Wpf.Native;
using Fenestra.Wpf.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Fenestra.Wpf;

/// <summary>
/// Fluent builder for configuring and launching a Fenestra WPF application.
/// </summary>
public class FenestraBuilder
{
    private readonly AppInfoBuilder _appInfoBuilder = new();
    private IWpfApplication? _wpfAppInstance;
    private string[]? _args;
    private Type? _windowPositionStorageType;
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
    /// Ignored when the application is running as a packaged app (MSIX/AppX).
    /// </summary>
    public FenestraBuilder UseAppName(string appName)
    {
        _appInfoBuilder.AppName = appName;
        return this;
    }

    /// <summary>
    /// Defines application info explicitly.
    /// Ignored when the application is running as a packaged app (MSIX/AppX).
    /// </summary>
    public FenestraBuilder UseAppInfo(string appName, Version version)
    {
        _appInfoBuilder.AppName = appName;
        _appInfoBuilder.Version = version;
        return this;
    }

    /// <summary>
    /// Defines application info explicitly with a custom application identifier.
    /// Ignored when the application is running as a packaged app (MSIX/AppX).
    /// </summary>
    public FenestraBuilder UseAppInfo(string appName, string appId, Version version)
    {
        _appInfoBuilder.AppName = appName;
        _appInfoBuilder.AppId = appId;
        _appInfoBuilder.Version = version;
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
    /// Uses a custom <see cref="IWindowPositionStorage"/> implementation.
    /// When not set, window positions are stored in the Windows Registry via <see cref="IRegistryConfig"/>.
    /// </summary>
    public FenestraBuilder UseWindowsPositionStorage<T>() where T : class, IWindowPositionStorage
    {
        _windowPositionStorageType = typeof(T);
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
        var appInfo = ResolveAppInfo();
        var host = BuildHost(shellType: ShellType, appInfo);
        var app = new FenestraApplication(host, appInfo, AppType, ShellType);
        _wpfAppInstance = app;
        return app;
    }

    /// <summary>
    /// Builds the primary host.
    /// Used internally by both FenestraApplication and FenestraApp.
    /// </summary>
    internal IHost BuildHost(Type? shellType)
        => BuildHost(shellType, ResolveAppInfo());

    private IHost BuildHost(Type? shellType, AppInfo appInfo)
    {

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

            var registryPath = $@"SOFTWARE\{appInfo.AppName}";
            var registryConfig = new Windows.Services.RegistryConfigService(registryPath);
            services.AddSingleton<IRegistryConfig>(registryConfig);

            if (appInfo.AppGuid == Guid.Empty)
            {
                var existing = registryConfig.Get<Guid>("AppGuid");
                if (existing != Guid.Empty)
                {
                    appInfo.AppGuid = existing;
                }
                else
                {
                    appInfo.AppGuid = Guid.NewGuid();
                    registryConfig.Set("AppGuid", appInfo.AppGuid);
                }
            }

            services.AddSingleton(appInfo);

            if (_windowPositionStorageType != null)
            {
                services.AddSingleton(typeof(IWindowPositionStorage), _windowPositionStorageType);
            }
            else
            {
                services.AddSingleton<IWindowPositionStorage, global::Fenestra.Windows.Services.RegistryWindowPositionStorage>();
            }

            services.AddSingleton<WindowStateService>();
            services.AddSingleton<IApplicationActivator, WpfApplicationActivator>();
            services.AddSingleton<IWindowManager, WindowManager>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IEventBus, EventBus>();
            services.AddSingleton<IThreadContext, WpfThreadContext>();
            services.AddSingleton<ITaskbarProvider, TaskbarProvider>();
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

    /// <summary>
    /// Resolves <see cref="AppInfo"/>. Package identity always takes precedence;
    /// user-configured values are used only for non-packaged apps.
    /// </summary>
    private AppInfo ResolveAppInfo()
    {
        var packaged = PackageIdentity.TryCreateAppInfo();
        if (packaged != null)
            return packaged;

        return _appInfoBuilder.FromEntryAssembly().Build();
    }
}
