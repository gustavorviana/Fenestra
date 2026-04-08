using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows.Models;
using Fenestra.Windows.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fenestra.Windows;

/// <summary>
/// Windows-specific builder extending <see cref="FenestraBuilder"/>.
/// Adds registry configuration, AppGuid persistence, and window position storage.
/// </summary>
public class WindowsFenestraBuilder : FenestraBuilder
{
    protected Type? _windowPositionStorageType;

    /// <summary>
    /// Uses a custom <see cref="IWindowPositionStorage"/> implementation.
    /// When not set, window positions are stored in the Windows Registry.
    /// </summary>
    public WindowsFenestraBuilder UseWindowsPositionStorage<T>() where T : class, IWindowPositionStorage
    {
        _windowPositionStorageType = typeof(T);
        return this;
    }

    protected override AppInfo ResolveAppInfo()
    {
        var builder = new WindowsAppInfoBuilder { AppName = AppName, AppId = AppId, Version = AppVersion };
        return builder.FromEntryAssembly().Build();
    }

    protected override void ConfigureHostServices(IServiceCollection services, AppInfo appInfo)
    {
        base.ConfigureHostServices(services, appInfo);

        // Register Windows-specific AppInfo
        if (appInfo is WindowsAppInfo windowsAppInfo)
            services.AddSingleton(windowsAppInfo);

        // Registry config
        var registryPath = $@"SOFTWARE\{appInfo.AppName}";
        var registryConfig = new RegistryConfigService(registryPath);
        services.AddSingleton<IRegistryConfig>(registryConfig);

        // AppGuid persistence (Windows Registry)
        if (appInfo is WindowsAppInfo windowsInfo && windowsInfo.AppGuid == Guid.Empty)
        {
            var existing = registryConfig.Get<Guid>("AppGuid");
            if (existing != Guid.Empty)
            {
                windowsInfo.AppGuid = existing;
            }
            else
            {
                windowsInfo.AppGuid = Guid.NewGuid();
                registryConfig.Set("AppGuid", windowsInfo.AppGuid);
            }
        }

        // Window position storage
        if (_windowPositionStorageType != null)
            services.AddSingleton(typeof(IWindowPositionStorage), _windowPositionStorageType);
        else
            services.AddSingleton<IWindowPositionStorage, RegistryWindowPositionStorage>();
    }
}
