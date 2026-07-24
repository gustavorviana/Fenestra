namespace Fenestra.Windows.Services;

/// <summary>
/// Options controlling how <see cref="RegistryConfigService"/> converts values.
/// </summary>
public sealed class RegistryConfigOptions
{
    /// <summary>
    /// Shared default: no custom converters.
    /// </summary>
    public static readonly RegistryConfigOptions Default = new();

    /// <summary>
    /// Custom converters, consulted (in registration order) before any built-in
    /// conversion. The first converter whose <see cref="IRegistryValueConverter.Type"/>
    /// is assignable from the value's type wins.
    /// </summary>
    public IList<IRegistryValueConverter> Converters { get; } = new List<IRegistryValueConverter>();
}
