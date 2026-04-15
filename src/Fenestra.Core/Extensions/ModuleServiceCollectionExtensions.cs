using Microsoft.Extensions.DependencyInjection;

namespace Fenestra.Core.Extensions;

/// <summary>
/// Internal helpers for registering Fenestra framework modules in the DI container.
/// A "module" is a service that implements <see cref="IFenestraModule"/> so the startup
/// pipeline can await its <see cref="IFenestraModule.InitAsync"/> before any UI materializes.
/// </summary>
internal static class ModuleServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TImplementation"/> as a singleton and exposes the
    /// same instance through <see cref="IFenestraModule"/> so the startup pipeline picks
    /// it up. Use this overload when the service has no public interface of its own
    /// (callers resolve it by its concrete type).
    /// </summary>
    public static IServiceCollection AddModule<TImplementation>(this IServiceCollection services)
        where TImplementation : class, IFenestraModule
    {
        services.AddSingleton<TImplementation>();
        services.AddSingleton<IFenestraModule>(sp => sp.GetRequiredService<TImplementation>());
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TImplementation"/> as a singleton and exposes the
    /// same instance through both <typeparamref name="TInterface"/> (the functional
    /// interface callers consume) and <see cref="IFenestraModule"/> (so the startup
    /// pipeline awaits its <see cref="IFenestraModule.InitAsync"/>).
    /// </summary>
    public static IServiceCollection AddModule<TInterface, TImplementation>(this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface, IFenestraModule
    {
        services.AddSingleton<TImplementation>();
        services.AddSingleton<TInterface>(sp => sp.GetRequiredService<TImplementation>());
        services.AddSingleton<IFenestraModule>(sp => sp.GetRequiredService<TImplementation>());
        return services;
    }
}
