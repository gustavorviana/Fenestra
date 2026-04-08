using Fenestra.Core.Models;
using Fenestra.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fenestra.Core;

/// <summary>
/// Base builder for configuring and launching a Fenestra application.
/// Platform-specific builders (Windows, WPF) extend this class.
/// </summary>
public class FenestraBuilder
{
    private readonly List<Action<ILoggingBuilder>> _loggingActions = new();
    private readonly List<Action<IServiceCollection>> _serviceActions = new();
    protected string[]? _args;

    /// <summary>Application name set by the user. Null means auto-detect from assembly.</summary>
    protected string? AppName { get; set; }
    /// <summary>Application ID set by the user. Null means derive from AppName.</summary>
    protected string? AppId { get; set; }
    /// <summary>Application version set by the user. Null means auto-detect from assembly.</summary>
    protected Version? AppVersion { get; set; }

    /// <summary>Service collection for the primary host.</summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <summary>Configuration for the primary host.</summary>
    public ConfigurationManager Configuration { get; } = new();

    /// <summary>The current environment name (e.g., Development, Production).</summary>
    public string Environment { get; set; } =
        System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? "Production";

    /// <summary>Sets the application name. Version is resolved from the entry assembly.</summary>
    public FenestraBuilder UseAppName(string appName)
    {
        AppName = appName;
        return this;
    }

    /// <summary>Defines application info explicitly.</summary>
    public FenestraBuilder UseAppInfo(string appName, Version version)
    {
        AppName = appName;
        AppVersion = version;
        return this;
    }

    /// <summary>Defines application info explicitly with a custom application identifier.</summary>
    public FenestraBuilder UseAppInfo(string appName, string appId, Version version)
    {
        AppName = appName;
        AppId = appId;
        AppVersion = version;
        return this;
    }

    /// <summary>Configures logging for the application.</summary>
    public FenestraBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        _loggingActions.Add(configure);
        return this;
    }

    /// <summary>Configures additional services.</summary>
    public FenestraBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        _serviceActions.Add(configure);
        return this;
    }

    /// <summary>Resolves application info. Override in platform-specific builders.</summary>
    protected virtual AppInfo ResolveAppInfo()
    {
        var builder = new AppInfoBuilder { AppName = AppName, AppId = AppId, Version = AppVersion };
        return builder.FromEntryAssembly().Build();
    }

    /// <summary>
    /// Registers core services (IEventBus, IExceptionHandler).
    /// Override in platform-specific builders to add platform services.
    /// </summary>
    protected virtual void ConfigureHostServices(IServiceCollection services, AppInfo appInfo)
    {
        services.AddSingleton(appInfo);
        services.AddSingleton<IAppInfo>(appInfo);
        services.AddSingleton<IEventBus, EventBus>();
        services.TryAddSingleton<IExceptionHandler, DefaultExceptionHandler>();
    }

    /// <summary>Builds the primary host with all configured services.</summary>
    protected IHost BuildHost()
    {
        var appInfo = ResolveAppInfo();
        return BuildHost(appInfo);
    }

    /// <summary>Builds the primary host with the given app info.</summary>
    protected IHost BuildHost(AppInfo appInfo)
    {
        var primaryHostBuilder = Host.CreateDefaultBuilder();

        primaryHostBuilder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddJsonFile($"appsettings.{Environment}.json", optional: true, reloadOnChange: true);
            config.AddEnvironmentVariables();

            foreach (var source in ((IConfigurationBuilder)Configuration).Sources)
                config.Add(source);

            if (_args is { Length: > 0 })
                config.AddCommandLine(_args);
        });

        primaryHostBuilder.ConfigureServices((_, services) =>
        {
            foreach (var descriptor in Services)
                services.Add(descriptor);

            ConfigureHostServices(services, appInfo);
        });

        primaryHostBuilder.ConfigureServices((_, services) =>
        {
            foreach (var action in _serviceActions)
                action(services);
        });

        primaryHostBuilder.ConfigureLogging(logging =>
        {
            foreach (var action in _loggingActions)
                action(logging);
        });

        return primaryHostBuilder.Build();
    }

    internal void SetArgs(string[] args) => _args = args;
}
