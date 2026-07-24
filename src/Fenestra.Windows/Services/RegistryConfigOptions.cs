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

    /// <summary>
    /// How <see cref="System.Enum"/> values are serialized. Defaults to
    /// <see cref="EnumStorageMode.Numeric"/> (REG_DWORD). Set to
    /// <see cref="EnumStorageMode.Name"/> to store the member name as REG_SZ.
    /// Reads accept either representation regardless of this setting.
    /// </summary>
    public EnumStorageMode EnumStorage { get; set; } = EnumStorageMode.Numeric;
}
