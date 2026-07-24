using Fenestra.Windows;
using Fenestra.Windows.Services;
using Microsoft.Win32;

namespace Fenestra.Windows.Tests.Services;

/// <summary>
/// Exercises <see cref="RegistryConfigService"/> custom-converter support against the
/// real HKCU registry under a disposable per-test key. No mocking: the point is to
/// verify real serialization (kind inference, string coercion, round-trip).
/// </summary>
public sealed class RegistryConfigServiceConverterTests
{
    // ── Test fixtures ────────────────────────────────────────────────

    /// <summary>Creates a unique HKCU key and deletes its subtree on dispose.</summary>
    private sealed class TempRegistry : IDisposable
    {
        private readonly string _path;
        public IRegistryConfig Config { get; }

        public TempRegistry(params IRegistryValueConverter[] converters)
        {
            _path = $@"SOFTWARE\FenestraTests\{Guid.NewGuid():N}";
            var options = new RegistryConfigOptions();
            foreach (var c in converters) options.Converters.Add(c);
            Config = new RegistryConfigService(_path, options);
        }

        public void Dispose()
        {
            Config.Dispose();
            try { Registry.CurrentUser.DeleteSubKeyTree(_path, throwOnMissingSubKey: false); }
            catch { /* best-effort cleanup */ }
        }
    }

    private readonly record struct Fraction(int Num, int Den);

    /// <summary>Round-trips <see cref="Fraction"/> as a "num/den" string.</summary>
    private sealed class FractionConverter : IRegistryValueConverter
    {
        public bool CanConvert(Type type) => type == typeof(Fraction);
        public object ToRegistry(object value) { var f = (Fraction)value; return $"{f.Num}/{f.Den}"; }
        public object ToClr(object raw)
        {
            var parts = ((string)raw).Split('/');
            return new Fraction(int.Parse(parts[0]), int.Parse(parts[1]));
        }
    }

    private interface IAnimal { string Sound { get; } }
    private sealed record Dog(string Sound) : IAnimal;

    /// <summary>Matches any <see cref="IAnimal"/> via assignability inside CanConvert.</summary>
    private sealed class AnimalConverter : IRegistryValueConverter
    {
        public bool CanConvert(Type type) => typeof(IAnimal).IsAssignableFrom(type);
        public object ToRegistry(object value) => ((IAnimal)value).Sound;
        public object ToClr(object raw) => new Dog((string)raw);
    }

    // ── Round-trip ───────────────────────────────────────────────────

    [Fact]
    public void Custom_converter_round_trips_value()
    {
        using var reg = new TempRegistry(new FractionConverter());

        reg.Config.Set("Ratio", new Fraction(3, 4));

        Assert.Equal(new Fraction(3, 4), reg.Config.Get<Fraction>("Ratio"));
    }

    [Fact]
    public void Custom_converter_stores_the_produced_string_verbatim()
    {
        using var reg = new TempRegistry(new FractionConverter());

        reg.Config.Set("Ratio", new Fraction(3, 4));

        // Raw registry value is the string the converter produced, stored as REG_SZ.
        Assert.Equal("3/4", reg.Config.GetValue("Ratio"));
    }

    // ── Precedence: converter beats built-in handling ────────────────

    [Fact]
    public void Converter_takes_precedence_over_builtin_type()
    {
        // Converter for int stores it as a string, overriding the built-in DWord path.
        var intAsString = new DelegateConverter(
            t => t == typeof(int),
            v => $"n={v}",
            raw => int.Parse(((string)raw).Substring(2)));

        using var reg = new TempRegistry(intAsString);

        reg.Config.Set("Count", 42);

        Assert.IsType<string>(reg.Config.GetValue("Count")); // stored as REG_SZ, not DWord
        Assert.Equal("n=42", reg.Config.GetValue("Count"));
        Assert.Equal(42, reg.Config.Get<int>("Count"));      // round-trips back through converter
    }

    // ── CanConvert assignability (subtype / interface) ───────────────

    [Fact]
    public void Converter_matches_via_assignable_interface()
    {
        using var reg = new TempRegistry(new AnimalConverter());

        reg.Config.Set("Pet", new Dog("woof"));

        Assert.Equal(new Dog("woof"), reg.Config.Get<Dog>("Pet"));
    }

    // ── Kind inference from converter output ─────────────────────────

    [Fact]
    public void Converter_output_kind_is_inferred_binary()
    {
        var toBytes = new DelegateConverter(
            t => t == typeof(Fraction),
            v => new byte[] { (byte)((Fraction)v).Num, (byte)((Fraction)v).Den },
            raw => { var b = (byte[])raw; return new Fraction(b[0], b[1]); });

        using var reg = new TempRegistry(toBytes);

        reg.Config.Set("Ratio", new Fraction(3, 4));

        Assert.IsType<byte[]>(reg.Config.GetValue("Ratio")); // REG_BINARY
        Assert.Equal(new Fraction(3, 4), reg.Config.Get<Fraction>("Ratio"));
    }

    [Fact]
    public void Converter_output_kind_is_inferred_dword()
    {
        var toInt = new DelegateConverter(
            t => t == typeof(Fraction),
            v => ((Fraction)v).Num * 100 + ((Fraction)v).Den,
            raw => { var n = (int)raw; return new Fraction(n / 100, n % 100); });

        using var reg = new TempRegistry(toInt);

        reg.Config.Set("Ratio", new Fraction(3, 4));

        Assert.IsType<int>(reg.Config.GetValue("Ratio")); // REG_DWORD
    }

    // ── Loop guard ───────────────────────────────────────────────────

    [Fact]
    public void Converter_returning_its_own_type_throws()
    {
        var loop = new DelegateConverter(
            t => t == typeof(Fraction),
            v => v,           // returns the same type → cannot infer a registry kind
            raw => raw);

        using var reg = new TempRegistry(loop);

        Assert.Throws<InvalidOperationException>(() => reg.Config.Set("Ratio", new Fraction(1, 2)));
    }

    // ── Sections + child propagation ─────────────────────────────────

    [RegistrySection]
    private sealed class Profile
    {
        public string Name { get; set; } = "";
        public Fraction Ratio { get; set; }
    }

    [Fact]
    public void Converter_applies_inside_sections()
    {
        using var reg = new TempRegistry(new FractionConverter());

        reg.Config.SetSection("Profile", new Profile { Name = "x", Ratio = new Fraction(5, 6) });

        var loaded = reg.Config.GetSection<Profile>("Profile");
        Assert.Equal("x", loaded.Name);
        Assert.Equal(new Fraction(5, 6), loaded.Ratio);
    }

    [Fact]
    public void Converter_propagates_to_child_config_sections()
    {
        using var reg = new TempRegistry(new FractionConverter());

        using var child = reg.Config.GetSection("Child", createIfNotExists: true)!;
        child.Set("Ratio", new Fraction(7, 8));

        Assert.Equal(new Fraction(7, 8), child.Get<Fraction>("Ratio"));
    }

    /// <summary>Converter built from delegates — keeps each test's intent local.</summary>
    private sealed class DelegateConverter(
        Func<Type, bool> canConvert,
        Func<object, object> toRegistry,
        Func<object, object> toClr) : IRegistryValueConverter
    {
        public bool CanConvert(Type type) => canConvert(type);
        public object ToRegistry(object value) => toRegistry(value);
        public object ToClr(object raw) => toClr(raw);
    }
}
