namespace Fenestra.Windows.Services;

/// <summary>
/// Converts a custom CLR type to and from a registry-storable value.
/// Registered converters are consulted <b>before</b> any built-in conversion,
/// so they can override the default handling of a type.
/// </summary>
/// <remarks>
/// <see cref="ToRegistry"/> may return any type the built-in pipeline already
/// understands (<see cref="string"/>, <see cref="int"/>, <c>byte[]</c>, etc.);
/// the resulting <see cref="Microsoft.Win32.RegistryValueKind"/> is inferred
/// automatically from that returned value.
/// </remarks>
public interface IRegistryValueConverter
{
    /// <summary>
    /// Returns <c>true</c> if this converter handles <paramref name="type"/>.
    /// The converter owns the compatibility rule — typically
    /// <c>typeof(Foo).IsAssignableFrom(type)</c> to also cover subclasses and
    /// interface implementations, but it may match by any criteria (open generics,
    /// attribute presence, etc.).
    /// </summary>
    bool CanConvert(Type type);

    /// <summary>
    /// Converts a CLR value into a registry-storable value (e.g. a <see cref="string"/>).
    /// </summary>
    object ToRegistry(object value);

    /// <summary>
    /// Converts a raw registry value back into the CLR type.
    /// </summary>
    object ToClr(object raw);
}
