using Fenestra.Core;
using Fenestra.Core.Extensions;
using Fenestra.Core.Models;
using Fenestra.Windows.Localization;
using Fenestra.Windows.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Resources;

namespace Fenestra.Windows.Extensions;

/// <summary>
/// Extension methods for registering Fenestra.Windows services in <see cref="IServiceCollection"/>.
/// These services are Windows-specific and will throw <see cref="PlatformNotSupportedException"/>
/// at runtime on non-Windows platforms.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Windows toast notification service (<see cref="IToastService"/>).
    /// Requires Windows 10 or later.
    /// </summary>
    public static IServiceCollection AddWindowsToastNotifications(this IServiceCollection services)
    {
        services.TryAddSingleton<IAumidRegistrationManager, AumidRegistrationManager>();
        services.AddSingleton<IToastService, ToastService>();
        return services;
    }

    /// <summary>
    /// Registers the Jump List service (<see cref="IJumpListService"/>) for customizing
    /// the taskbar icon's right-click menu with custom tasks and recent files. Backed
    /// directly by the Win32 <c>ICustomDestinationList</c> COM API — no WPF dependency.
    /// Transitively registers <see cref="IAumidRegistrationManager"/> which Jump Lists
    /// need for the AUMID + Start Menu shortcut prerequisites. Windows only.
    /// </summary>
    public static IServiceCollection AddWindowsJumpList(this IServiceCollection services)
    {
        services.TryAddSingleton<IAumidRegistrationManager, AumidRegistrationManager>();
        services.AddSingleton<IJumpListService, JumpListService>();
        return services;
    }

    /// <summary>
    /// Registers the taskbar overlay icon service (<see cref="ITaskbarOverlayService"/>)
    /// for displaying a small badge icon on the application's taskbar button. Backed by
    /// <c>ITaskbarList3::SetOverlayIcon</c>. Framework-agnostic — works from any host.
    /// For WPF <c>ImageSource</c> inputs, add <c>using Fenestra.Wpf;</c> to bring in the
    /// WPF bridge extension. Windows 7+.
    /// </summary>
    public static IServiceCollection AddWindowsTaskbarOverlay(this IServiceCollection services)
    {
        services.AddSingleton<ITaskbarOverlayService, TaskbarOverlayService>();
        return services;
    }

    /// <summary>
    /// Registers the toast background activation service (<see cref="IToastActivationRegistrar"/>).
    /// Requires <see cref="AddWindowsToastNotifications"/> to be called first.
    /// Requires Windows 10 or later.
    /// </summary>
    public static IServiceCollection AddWindowsToastActivation(this IServiceCollection services)
    {
        services.AddModule<IToastActivationRegistrar, ToastActivationRegistrar>();
        return services;
    }

    /// <summary>
    /// Registers the Windows auto-start service (<see cref="IAutoStartService"/>).
    /// Manages application startup registration via the Windows Registry.
    /// </summary>
    public static IServiceCollection AddWindowsAutoStart(this IServiceCollection services)
    {
        services.AddSingleton<IAutoStartService, AutoStartService>();
        return services;
    }

    /// <summary>
    /// Registers the Windows theme detection service (<see cref="IThemeService"/>).
    /// Monitors dark/light mode changes. Requires Windows 10 or later.
    /// </summary>
    public static IServiceCollection AddWindowsThemeDetection(this IServiceCollection services)
    {
        services.AddSingleton<IThemeService, ThemeService>();
        return services;
    }

    /// <summary>
    /// Registers the Windows Credential Vault service (<see cref="ICredentialVault"/>).
    /// Provides DPAPI-encrypted per-user storage for secrets via the Credential Manager.
    /// Requires Windows. See <c>docs/credential-vault.md</c> for the security model.
    /// </summary>
    public static IServiceCollection AddWindowsCredentialVault(this IServiceCollection services)
    {
        services.AddSingleton<ICredentialVault, CredentialVault>();
        return services;
    }

    /// <summary>
    /// Registers the idle detection service (<see cref="IIdleDetectionService"/>).
    /// Polls <c>GetLastInputInfo</c> on a pool-based <see cref="System.Threading.Timer"/> and
    /// raises <c>BecameIdle</c>/<c>BecameActive</c> events on transitions. Framework-agnostic —
    /// works in WPF, WinForms, console, and background Windows hosts. If an
    /// <see cref="Core.IThreadContext"/> is registered (e.g., by <c>WpfFenestraBuilder</c>),
    /// events are marshalled to the dispatcher thread automatically.
    /// </summary>
    public static IServiceCollection AddWindowsIdleDetection(
        this IServiceCollection services,
        Action<IdleDetectionOptions>? configure = null)
    {
        var options = new IdleDetectionOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<IIdleDetectionService, IdleDetectionService>();
        return services;
    }

    /// <summary>
    /// Registers the app lifecycle service (<see cref="IAppLifecycleService"/>).
    /// Tracks first run, version upgrades, install date, and launch count via the registry.
    /// Must be resolved at startup (typically in <c>OnReady</c>) so that <c>LaunchCount</c>
    /// increments once per launch. See <c>docs/app-lifecycle.md</c>.
    /// </summary>
    public static IServiceCollection AddWindowsAppLifecycle(this IServiceCollection services)
    {
        services.AddSingleton<IAppLifecycleService, AppLifecycleService>();
        return services;
    }

    /// <summary>
    /// Registers the localization service (<see cref="ILocalizationService"/>). Reads the
    /// persisted culture at startup (falling back to OS culture, then the configured
    /// default), applies it to the process, and allows runtime changes via
    /// <see cref="ILocalizationService.SetCulture"/>. Must be resolved at startup so the
    /// culture is applied before any UI materializes. See <c>docs/localization.md</c>.
    /// </summary>
    public static IServiceCollection AddWindowsLocalization(
        this IServiceCollection services,
        Action<LocalizationOptions> configure)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        var options = new LocalizationOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddModule<ILocalizationService, LocalizationService>();
        RegisterResources(options);

        return services;
    }

    private static void RegisterResources(LocalizationOptions options)
    {
        var translationSource = TranslationSource.Instance;

        foreach (var res in options.Resources)
        {
            LocalizationRegistryBuilder.Register(
                translationSource,
                alias: res.Alias.ToLowerInvariant(),
                manager: new ResourceManager(res.BaseName, res.Assembly),
                baseName: res.BaseName,
                behavior: options.DuplicateBehavior);
        }

        foreach (var def in options.AutoDiscoverAssemblies)
        {
            var manifestNames = def.Assembly.GetManifestResourceNames();

            foreach (var (baseName, alias) in LocalizationRegistryBuilder.EnumerateAutoDiscovered(
                         manifestNames, def.Prefix, def.NamespaceFilter))
            {
                LocalizationRegistryBuilder.Register(
                    translationSource,
                    alias: alias,
                    manager: new ResourceManager(baseName, def.Assembly),
                    baseName: baseName,
                    behavior: options.DuplicateBehavior);
            }
        }
    }
}
