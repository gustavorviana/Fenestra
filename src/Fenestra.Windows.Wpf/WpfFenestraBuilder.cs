using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows;
using Fenestra.Wpf.Extensions;
using Fenestra.Wpf.Native;
using Fenestra.Wpf.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Fenestra.Wpf;

/// <summary>
/// WPF-specific builder extending <see cref="WindowsFenestraBuilder"/>.
/// Adds WPF window management, dialogs, taskbar, and application lifecycle.
/// </summary>
public class WpfFenestraBuilder : WindowsFenestraBuilder
{
    private IWpfApplication? _wpfAppInstance;

    /// <summary>The consumer's Application type (set by CreateBuilder). Null means default Application.</summary>
    internal Type? AppType { get; set; }

    /// <summary>The main window type.</summary>
    internal Type? ShellType { get; set; }

    /// <summary>Splash screen configuration</summary>
    private Type? _splashScreenType = null;

    public WpfFenestraBuilder()
    {
        Services.AddSingleton<IWindowManager, WindowManager>();
        Services.AddSingleton<IDialogService, DialogService>();
    }

    /// <summary>Sets the main window type for the application.</summary>
    public WpfFenestraBuilder UseMainWindow<TShell>() where TShell : Window
    {
        ShellType = typeof(TShell);
        return this;
    }

    /// <summary>Registers a factory that dynamically creates the main window.</summary>
    public WpfFenestraBuilder UseMainWindowFactory<TFactory>() where TFactory : class, IMainWindowFactory
    {
        Services.AddSingleton<IMainWindowFactory, TFactory>();
        return this;
    }

    /// <summary>
    /// Enables global exception handling using the built-in <see cref="DefaultExceptionHandler"/>,
    /// which logs the exception and shows a message box to the user.
    /// </summary>
    public WpfFenestraBuilder UseErrorHandler()
    {
        Services.TryAddSingleton<IExceptionHandler, DefaultExceptionHandler>();
        return this;
    }

    /// <summary>
    /// Enables global exception handling using a custom <see cref="IExceptionHandler"/> type
    /// resolved from the DI container.
    /// </summary>
    /// <typeparam name="T">The <see cref="IExceptionHandler"/> implementation to register.</typeparam>
    public WpfFenestraBuilder UseErrorHandler<T>() where T : class, IExceptionHandler
    {
        Services.TryAddSingleton<IExceptionHandler, T>();
        return this;
    }

    /// <summary>
    /// Enables global exception handling using a pre-constructed <see cref="IExceptionHandler"/> instance.
    /// </summary>
    /// <typeparam name="T">The <see cref="IExceptionHandler"/> implementation type.</typeparam>
    /// <param name="instance">The handler instance to use as a singleton.</param>
    public WpfFenestraBuilder UseErrorHandler<T>(T instance) where T : class, IExceptionHandler
    {
        Services.TryAddSingleton<IExceptionHandler>(instance);
        return this;
    }

    /// <summary>Registers all Window types from the assembly containing TMarker.</summary>
    public WpfFenestraBuilder RegisterWindows<TMarker>()
    {
        Services.RegisterWindows<TMarker>();
        return this;
    }

    /// <summary>Registers all Window types from the assemblies containing the specified marker types.</summary>
    public WpfFenestraBuilder RegisterWindows(params Type[] markers)
    {
        Services.RegisterWindows(markers);
        return this;
    }

    /// <summary>Registers all Window types via assembly scan.</summary>
    public WpfFenestraBuilder RegisterWindows()
    {
        Services.RegisterWindows();
        return this;
    }

    /// <summary>Builds the FenestraApplication.</summary>
    public FenestraApplication Build()
    {
        var appInfo = ResolveAppInfo();
        var host = BuildHost(appInfo);
        var app = new FenestraApplication(host, appInfo, AppType, ShellType);
        _wpfAppInstance = app;
        return app;
    }

    /// <summary>Builds the primary host. Used internally by FenestraApp.</summary>
    internal IHost BuildHostInternal()
        => BuildHost(ResolveAppInfo());

    internal void SetWpfAppInstance(IWpfApplication instance) => _wpfAppInstance = instance;

    protected override AppInfo ResolveAppInfo()
    {
        // Packaged app (MSIX) identity takes precedence
        var packaged = PackageIdentity.TryCreateAppInfo();
        if (packaged != null)
            return packaged;

        return base.ResolveAppInfo();
    }

    public WpfFenestraBuilder UseSplashScreen<TSplash>() where TSplash : class, ISplashScreen
    {
        _splashScreenType = typeof(TSplash);
        Services.AddTransient<TSplash>();
        Services.AddTransient<ISplashScreen>(sp => sp.GetRequiredService<TSplash>());
        return this;
    }

    protected override void ConfigureHostServices(IServiceCollection services, AppInfo appInfo)
    {
        base.ConfigureHostServices(services, appInfo);

        // WPF-specific services
        services.AddSingleton<WindowStateService>();
        services.AddSingleton<IApplicationActivator, WpfApplicationActivator>();
        services.AddSingleton<IWindowManager, WindowManager>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IThreadContext, WpfThreadContext>();
        services.AddSingleton<ITaskbarProvider, TaskbarProvider>();
        services.AddSingleton<IWpfApplication>(_ => _wpfAppInstance!);

        if (_splashScreenType != null)
        {
            services.AddSingleton(sp => new WpfSplashCoordinator(_splashScreenType, sp));
            services.AddSingleton<IProgress<SplashStatus>>(sp => sp.GetRequiredService<WpfSplashCoordinator>());
        }

        if (ShellType != null)
            services.AddTransient(ShellType);
    }
}
