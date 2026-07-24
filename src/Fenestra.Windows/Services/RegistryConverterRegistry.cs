using System.Collections.Concurrent;

namespace Fenestra.Windows.Services;

/// <summary>
/// Internal resolver that maps a CLR type to the first registered
/// <see cref="IRegistryValueConverter"/> whose <see cref="IRegistryValueConverter.CanConvert"/>
/// accepts it, caching each resolution (including misses) so the linear scan
/// runs at most once per type.
/// </summary>
internal sealed class RegistryConverterRegistry
{
    /// <summary>Shared empty registry — no converters, resolves everything to <c>null</c>.</summary>
    public static readonly RegistryConverterRegistry Empty = new(Array.Empty<IRegistryValueConverter>());

    private readonly IRegistryValueConverter[] _converters;
    private readonly ConcurrentDictionary<Type, IRegistryValueConverter?> _cache = new();

    public RegistryConverterRegistry(IEnumerable<IRegistryValueConverter> converters)
        => _converters = converters as IRegistryValueConverter[] ?? converters.ToArray();

    /// <summary>
    /// Returns the converter handling <paramref name="type"/>, or <c>null</c> if none.
    /// The first converter (in registration order) whose
    /// <see cref="IRegistryValueConverter.CanConvert"/> accepts
    /// <paramref name="type"/> wins; results are cached per type.
    /// </summary>
    public IRegistryValueConverter? Resolve(Type type)
        => _cache.GetOrAdd(type, Find);

    private IRegistryValueConverter? Find(Type type)
    {
        foreach (var c in _converters)
            if (c.CanConvert(type))
                return c;
        return null;
    }
}
