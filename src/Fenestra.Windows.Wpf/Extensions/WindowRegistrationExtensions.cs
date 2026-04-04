using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Windows;

namespace Fenestra.Wpf.Extensions;

/// <summary>
/// Extension methods for registering Window types in the DI container.
/// </summary>
public static class WindowRegistrationExtensions
{
    /// <summary>
    /// Registers all Window types from the assembly containing <typeparamref name="TMarker"/> as transient services.
    /// </summary>
    public static IServiceCollection RegisterWindows<TMarker>(this IServiceCollection services)
    {
        return services.RegisterWindows(typeof(TMarker).Assembly);
    }

    /// <summary>
    /// Registers all Window types from the assemblies containing the specified marker types as transient services.
    /// </summary>
    public static IServiceCollection RegisterWindows(this IServiceCollection services, params Type[] markers)
    {
        var assemblies = markers.Select(m => m.Assembly).Distinct();

        foreach (var assembly in assemblies)
        {
            services.RegisterWindows(assembly);
        }

        return services;
    }

    /// <summary>
    /// Registers all Window types from all loaded non-framework assemblies as transient services.
    /// </summary>
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
