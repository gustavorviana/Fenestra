namespace Fenestra.Windows;

/// <summary>
/// Provides typed read/write access to Windows Registry keys.
/// Supports primitive values, common .NET types, and structured sections
/// where types decorated with <see cref="RegistrySectionAttribute"/> are stored as subkeys.
/// </summary>
public interface IRegistryConfig : IDisposable
{
    /// <summary>
    /// Sets a value in the current key. Pass <c>null</c> to delete the value.
    /// </summary>
    void Set(string name, object? value);

    /// <summary>
    /// Gets a typed value from the current key. Returns <c>default</c> if the value does not exist.
    /// Throws on conversion failure.
    /// </summary>
    T? Get<T>(string name);

    /// <summary>
    /// Tries to get a typed value from the current key.
    /// Returns <c>false</c> if the value does not exist or conversion fails.
    /// </summary>
    bool TryGet<T>(string name, out T? value);

    /// <summary>
    /// Gets a string value from the current key, or <paramref name="defaultValue"/> if not found.
    /// </summary>
    string? GetString(string name, string? defaultValue = null);

    /// <summary>
    /// Gets the raw registry value, or <paramref name="defaultValue"/> if not found.
    /// </summary>
    object? GetValue(string name, object? defaultValue = null);

    /// <summary>
    /// Reads all properties of <typeparamref name="T"/> from the specified subkey.
    /// Properties whose type has <see cref="RegistrySectionAttribute"/> are read recursively from nested subkeys.
    /// Throws on conversion failure.
    /// </summary>
    T GetSection<T>(string sectionName) where T : new();

    /// <summary>
    /// Tries to read all properties of <typeparamref name="T"/> from the specified subkey.
    /// Returns <c>false</c> if the subkey does not exist or reading fails.
    /// </summary>
    bool TryGetSection<T>(string sectionName, out T? value) where T : new();

    /// <summary>
    /// Writes all readable properties of <paramref name="section"/> into the specified subkey.
    /// Properties whose type has <see cref="RegistrySectionAttribute"/> are written recursively into nested subkeys.
    /// </summary>
    void SetSection(string sectionName, object section);

    /// <summary>
    /// Returns a child <see cref="IRegistryConfig"/> wrapping the specified subkey.
    /// Returns <c>null</c> if the subkey does not exist and <paramref name="createIfNotExists"/> is <c>false</c>.
    /// </summary>
    IRegistryConfig? GetSection(string sectionName, bool createIfNotExists = false);

    /// <summary>
    /// Returns <c>true</c> if a value with the specified name exists in the current key.
    /// </summary>
    bool Exists(string name);

    /// <summary>
    /// Returns the names of all values in the current key.
    /// </summary>
    string[] GetValueNames();

    /// <summary>
    /// Returns the names of all subkeys under the current key.
    /// </summary>
    string[] GetSections();

    /// <summary>
    /// Deletes a subkey and all its values. Returns <c>false</c> if the subkey did not exist.
    /// </summary>
    bool DeleteSection(string sectionName);
}
