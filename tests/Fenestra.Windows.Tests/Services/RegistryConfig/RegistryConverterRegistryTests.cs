using Fenestra.Windows.Services;

namespace Fenestra.Windows.Tests.Services.RegistryConfig;

/// <summary>
/// Unit tests for the internal <see cref="RegistryConverterRegistry"/> resolver —
/// registration-order precedence and per-type resolution caching. No registry access.
/// </summary>
public sealed class RegistryConverterRegistryTests
{
    /// <summary>Counts how many times <see cref="CanConvert"/> is invoked.</summary>
    private sealed class CountingConverter(Func<Type, bool> predicate) : IRegistryValueConverter
    {
        public int Calls { get; private set; }

        public bool CanConvert(Type type) { Calls++; return predicate(type); }
        public object ToRegistry(object value) => value.ToString()!;
        public object ToClr(object raw) => raw;
    }

    [Fact]
    public void Empty_resolves_to_null()
    {
        Assert.Null(RegistryConverterRegistry.Empty.Resolve(typeof(int)));
    }

    [Fact]
    public void No_match_resolves_to_null()
    {
        var registry = new RegistryConverterRegistry([new CountingConverter(_ => false)]);

        Assert.Null(registry.Resolve(typeof(int)));
    }

    [Fact]
    public void First_matching_converter_in_registration_order_wins()
    {
        var first = new CountingConverter(_ => true);
        var second = new CountingConverter(_ => true);
        var registry = new RegistryConverterRegistry([first, second]);

        Assert.Same(first, registry.Resolve(typeof(int)));
    }

    [Fact]
    public void Hit_is_cached_scan_runs_once_per_type()
    {
        var conv = new CountingConverter(t => t == typeof(int));
        var registry = new RegistryConverterRegistry([conv]);

        registry.Resolve(typeof(int));
        registry.Resolve(typeof(int));

        Assert.Equal(1, conv.Calls); // second Resolve served from cache
    }

    [Fact]
    public void Miss_is_cached_scan_runs_once_per_type()
    {
        var conv = new CountingConverter(_ => false);
        var registry = new RegistryConverterRegistry([conv]);

        registry.Resolve(typeof(int));
        registry.Resolve(typeof(int));

        Assert.Equal(1, conv.Calls); // negative result cached too
    }

    [Fact]
    public void Distinct_types_are_resolved_independently()
    {
        var conv = new CountingConverter(t => t == typeof(int));
        var registry = new RegistryConverterRegistry([conv]);

        Assert.Same(conv, registry.Resolve(typeof(int)));
        Assert.Null(registry.Resolve(typeof(string)));
        Assert.Equal(2, conv.Calls); // one scan per distinct type
    }
}
