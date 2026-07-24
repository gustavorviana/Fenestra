using Fenestra.Windows.Services;
using Fenestra.Windows.Tests.Utils;
using System.ComponentModel;
using System.Globalization;

namespace Fenestra.Windows.Tests.Services.RegistryConfig;

/// <summary>
/// Exercises the convention-based nested-section detection of
/// <see cref="RegistryConfigService"/>: complex types become subkeys automatically
/// (no <see cref="RegistrySectionAttribute"/> required), while storable/scalar types
/// stay single values. Runs against the real HKCU registry under a disposable key.
/// </summary>
public sealed class RegistryConfigServiceSectionDetectionTests
{
    private sealed class Server
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
    }

    private sealed class AppConfig
    {
        public string Name { get; set; } = "";
        public Server Primary { get; set; } = new();
    }

    // ── Automatic nested section ─────────────────────────────────────

    [Fact]
    public void Complex_property_is_stored_as_subkey_without_attribute()
    {
        using var reg = new TempRegistry();

        reg.Config.SetSection("App", new AppConfig
        {
            Name = "svc",
            Primary = new Server { Host = "db.local", Port = 5432 }
        });

        using var app = reg.Config.GetSection("App")!;
        Assert.Contains("Primary", app.GetSections());          // nested object → subkey
        Assert.Contains("Name", app.GetValueNames());           // scalar sibling stays a value
        Assert.DoesNotContain("Primary", app.GetValueNames());  // not stored as a value
    }

    [Fact]
    public void Complex_property_round_trips_without_attribute()
    {
        using var reg = new TempRegistry();

        reg.Config.SetSection("App", new AppConfig
        {
            Name = "svc",
            Primary = new Server { Host = "db.local", Port = 5432 }
        });

        var loaded = reg.Config.GetSection<AppConfig>("App");
        Assert.Equal("svc", loaded.Name);
        Assert.Equal("db.local", loaded.Primary.Host);
        Assert.Equal(5432, loaded.Primary.Port);
    }

    // ── Deep nesting ─────────────────────────────────────────────────

    private sealed class Level3 { public int Value { get; set; } }
    private sealed class Level2 { public Level3 Inner { get; set; } = new(); }
    private sealed class Level1 { public Level2 Mid { get; set; } = new(); }

    [Fact]
    public void Nested_sections_recurse_arbitrarily_deep()
    {
        using var reg = new TempRegistry();

        reg.Config.SetSection("Root", new Level1 { Mid = new Level2 { Inner = new Level3 { Value = 99 } } });

        var loaded = reg.Config.GetSection<Level1>("Root");
        Assert.Equal(99, loaded.Mid.Inner.Value);

        using var root = reg.Config.GetSection("Root")!;
        using var mid = root.GetSection("Mid")!;
        Assert.Contains("Inner", mid.GetSections());
    }

    // ── Struct becomes a section (has no explicit parameterless ctor) ─

    private struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    private sealed class Shape
    {
        public Point Origin { get; set; }
    }

    [Fact]
    public void Struct_property_is_treated_as_section()
    {
        using var reg = new TempRegistry();

        reg.Config.SetSection("Shape", new Shape { Origin = new Point { X = 3, Y = 4 } });

        using var shape = reg.Config.GetSection("Shape")!;
        Assert.Contains("Origin", shape.GetSections());

        var loaded = reg.Config.GetSection<Shape>("Shape");
        Assert.Equal(3, loaded.Origin.X);
        Assert.Equal(4, loaded.Origin.Y);
    }

    // ── Known/scalar types stay values, never subkeys ────────────────

    private enum Hue { Red = 1, Green = 2 }

    private sealed class Scalars
    {
        public Guid Id { get; set; }
        public TimeSpan Span { get; set; }
        public Uri Url { get; set; } = new("https://example.com");
        public decimal Amount { get; set; }
        public DateTime When { get; set; }
        public Hue Color { get; set; }
    }

    [Fact]
    public void Known_types_are_stored_as_values_not_subkeys()
    {
        using var reg = new TempRegistry();

        reg.Config.SetSection("S", new Scalars
        {
            Id = Guid.NewGuid(),
            Span = TimeSpan.FromMinutes(5),
            Url = new Uri("https://fenestra.dev"),
            Amount = 12.5m,
            When = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Color = Hue.Green
        });

        using var s = reg.Config.GetSection("S")!;
        Assert.Empty(s.GetSections()); // none of these are complex → no subkeys
        foreach (var name in new[] { "Id", "Span", "Url", "Amount", "When", "Color" })
            Assert.Contains(name, s.GetValueNames());
    }

    // ── Type with a string TypeConverter stays a value ───────────────

    [TypeConverter(typeof(TemperatureConverter))]
    private readonly struct Temperature
    {
        public Temperature(int celsius) => Celsius = celsius;
        public int Celsius { get; }
    }

    private sealed class TemperatureConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? c, Type t) => t == typeof(string);
        public override bool CanConvertTo(ITypeDescriptorContext? c, Type? t) => t == typeof(string);
        public override object ConvertFrom(ITypeDescriptorContext? c, CultureInfo? ci, object v)
            => new Temperature(int.Parse((string)v, CultureInfo.InvariantCulture));
        public override object ConvertTo(ITypeDescriptorContext? c, CultureInfo? ci, object? v, Type t)
            => ((Temperature)v!).Celsius.ToString(CultureInfo.InvariantCulture);
    }

    private sealed class Weather
    {
        public Temperature Current { get; set; }
    }

    [Fact]
    public void Type_with_string_typeconverter_stays_a_value()
    {
        using var reg = new TempRegistry();

        reg.Config.SetSection("W", new Weather { Current = new Temperature(21) });

        using var w = reg.Config.GetSection("W")!;
        Assert.Empty(w.GetSections());                 // TypeConverter → value, not section
        Assert.Contains("Current", w.GetValueNames());
        Assert.Equal("21", w.GetValue("Current"));     // stored via the TypeConverter

        var loaded = reg.Config.GetSection<Weather>("W");
        Assert.Equal(21, loaded.Current.Celsius);
    }

    // ── Custom converter beats convention (complex type → value) ─────

    private sealed class Money
    {
        public int Cents { get; set; }
    }

    private sealed class MoneyConverter : IRegistryValueConverter
    {
        public bool CanConvert(Type type) => type == typeof(Money);
        public object ToRegistry(object value) => ((Money)value).Cents;
        public object ToClr(object raw) => new Money { Cents = Convert.ToInt32(raw) };
    }

    private sealed class Wallet
    {
        public Money Balance { get; set; } = new();
    }

    [Fact]
    public void Registered_converter_makes_complex_type_a_value()
    {
        using var reg = new TempRegistry(new MoneyConverter());

        reg.Config.SetSection("Wallet", new Wallet { Balance = new Money { Cents = 999 } });

        using var wallet = reg.Config.GetSection("Wallet")!;
        Assert.Empty(wallet.GetSections());               // converter wins → no subkey
        Assert.Contains("Balance", wallet.GetValueNames());

        var loaded = reg.Config.GetSection<Wallet>("Wallet");
        Assert.Equal(999, loaded.Balance.Cents);
    }

    // ── Explicit attribute still forces a section ────────────────────

    [RegistrySection]
    private sealed class Tagged
    {
        public int N { get; set; }
    }

    private sealed class HasTagged
    {
        public Tagged Child { get; set; } = new();
    }

    [Fact]
    public void Attribute_still_forces_section_behavior()
    {
        using var reg = new TempRegistry();

        reg.Config.SetSection("Parent", new HasTagged { Child = new Tagged { N = 7 } });

        using var parent = reg.Config.GetSection("Parent")!;
        Assert.Contains("Child", parent.GetSections());

        Assert.Equal(7, reg.Config.GetSection<HasTagged>("Parent").Child.N);
    }

    // ── Complex type with no parameterless ctor falls to value pipeline ─

    private sealed class NoDefaultCtor
    {
        public NoDefaultCtor(int v) => V = v;
        public int V { get; }
    }

    private sealed class HasNoDefaultCtor
    {
        public NoDefaultCtor Child { get; set; } = new(1);
    }

    [Fact]
    public void Complex_type_without_parameterless_ctor_is_not_a_section()
    {
        using var reg = new TempRegistry();

        // Not a section (no ctor to instantiate) → falls to the value pipeline, which
        // stores it as its ToString() via the base TypeConverter (no subkey created).
        reg.Config.SetSection("X", new HasNoDefaultCtor());

        using var x = reg.Config.GetSection("X")!;
        Assert.Empty(x.GetSections());
        Assert.Contains("Child", x.GetValueNames());

        // The stored string cannot be converted back (no CanConvertFrom(string)).
        Assert.Throws<NotSupportedException>(() => reg.Config.GetSection<HasNoDefaultCtor>("X"));
    }
}
