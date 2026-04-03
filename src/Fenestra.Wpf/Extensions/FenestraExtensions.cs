using Fenestra.Core;
using Fenestra.Wpf.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fenestra.Wpf.Extensions;

public static class FenestraExtensions
{
    public static IServiceCollection AddFenestra(this IServiceCollection services)
    {
        services.AddSingleton<IWindowManager, WindowManager>();
        services.AddSingleton<IDialogService, DialogService>();
        services.TryAddSingleton<IExceptionHandler, DefaultExceptionHandler>();

        return services;
    }
}
