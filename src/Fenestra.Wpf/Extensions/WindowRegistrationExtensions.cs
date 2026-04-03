using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Fenestra.Wpf.Extensions;

public static class WindowRegistrationExtensions
{
    public static IServiceCollection RegisterWindows<TMarker>(this IServiceCollection services)
    {
        return services.RegisterWindows(typeof(TMarker).Assembly);
    }

    public static IServiceCollection RegisterWindows(this IServiceCollection services, params Type[] markers)
    {
        var assemblies = markers.Select(m => m.Assembly).Distinct();

        foreach (var assembly in assemblies)
        {
            services.RegisterWindows(assembly);
        }

        return services;
    }

    public static IServiceCollection RegisterWindows(this IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !a.FullName!.StartsWith("System") && !a.FullName.StartsWith("Microsoft"));

        foreach (var assembly in assemblies)
        {
            services.RegisterWindows(assembly);
        }

        return services;
    }

    private static IServiceCollection RegisterWindows(this IServiceCollection services, Assembly assembly)
    {
        var windowTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Window).IsAssignableFrom(t));

        foreach (var windowType in windowTypes)
        {
            services.AddTransient(windowType);
        }

        return services;
    }
}
