using Microsoft.Win32;

namespace Fenestra.Windows.Services;

/// <summary>
/// Factory for <see cref="IRegistryConfig"/> instances targeting any location in the Windows Registry.
/// Supports all hives (HKCU, HKLM, HKCR, HKU, HKCC) and both 32/64-bit registry views.
/// </summary>
public interface IRegistryConfigFactory
{
    /// <summary>
    /// Opens or creates a registry key under the specified hive.
    /// </summary>
    /// <param name="hive">The registry hive (e.g. <see cref="RegistryHive.CurrentUser"/>).</param>
    /// <param name="keyPath">Subkey path below the hive (e.g. <c>SOFTWARE\MyApp</c>).</param>
    /// <param name="view">Registry view (32-bit, 64-bit, or default).</param>
    IRegistryConfig OpenOrCreate(RegistryHive hive, string keyPath, RegistryView view = RegistryView.Default);

    /// <summary>
    /// Opens an existing registry key under the specified hive.
    /// Returns <c>null</c> if the key does not exist.
    /// </summary>
    /// <param name="hive">The registry hive.</param>
    /// <param name="keyPath">Subkey path below the hive.</param>
    /// <param name="writable">Whether the key should be opened with write access.</param>
    /// <param name="view">Registry view (32-bit, 64-bit, or default).</param>
    IRegistryConfig? Open(RegistryHive hive, string keyPath, bool writable = true, RegistryView view = RegistryView.Default);

    /// <summary>
    /// Opens or creates a registry key from a full path such as
    /// <c>HKCU\SOFTWARE\MyApp</c> or <c>HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Foo</c>.
    /// </summary>
    /// <param name="fullPath">Full registry path, prefixed by the hive short or long name.</param>
    /// <param name="view">Registry view (32-bit, 64-bit, or default).</param>
    IRegistryConfig OpenOrCreate(string fullPath, RegistryView view = RegistryView.Default);

    /// <summary>
    /// Opens an existing registry key from a full path such as <c>HKCU\SOFTWARE\MyApp</c>.
    /// Returns <c>null</c> if the key does not exist.
    /// </summary>
    /// <param name="fullPath">Full registry path, prefixed by the hive short or long name.</param>
    /// <param name="writable">Whether the key should be opened with write access.</param>
    /// <param name="view">Registry view (32-bit, 64-bit, or default).</param>
    IRegistryConfig? Open(string fullPath, bool writable = true, RegistryView view = RegistryView.Default);
}
