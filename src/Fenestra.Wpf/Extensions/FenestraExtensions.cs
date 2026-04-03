using Fenestra.Core;
using Fenestra.Wpf.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fenestra.Wpf.Extensions;

/// <summary>
/// Extension methods for registering core Fenestra services in the DI container.
/// </summary>
public static class FenestraExtensions
{
    /// <summary>
    /// Registers core Fenestra services (window manager, dialog service, exception handler) in the container.
    /// </summary>
    public static IServiceCollection AddFenestra(this IServiceCollection services)
    {
        services.AddSingleton<IWindowManager, WindowManager>();
        services.AddSingleton<IDialogService, DialogService>();
        services.TryAddSingleton<IExceptionHandler, DefaultExceptionHandler>();

        return services;
    }
}
