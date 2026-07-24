namespace Fenestra.Windows.Services;

/// <summary>
/// Controls how <see cref="RegistryConfigService"/> serializes <see cref="System.Enum"/> values.
/// </summary>
public enum EnumStorageMode
{
    /// <summary>
    /// Store the underlying numeric value as a REG_DWORD (default; back-compatible).
    /// </summary>
    Numeric = 0,

    /// <summary>
    /// Store the member name as a REG_SZ string; reads are case-sensitive.
    /// </summary>
    Name = 1,

    /// <summary>
    /// Store the member name as a REG_SZ string; reads are case-insensitive.
    /// </summary>
    NameIgnoreCase = 2,
}
