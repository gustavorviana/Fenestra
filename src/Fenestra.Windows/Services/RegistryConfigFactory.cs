using Microsoft.Win32;

namespace Fenestra.Windows.Services;

/// <summary>
/// Default <see cref="IRegistryConfigFactory"/> implementation.
/// </summary>
public sealed class RegistryConfigFactory : IRegistryConfigFactory
{
    public IRegistryConfig OpenOrCreate(RegistryHive hive, string keyPath, RegistryView view = RegistryView.Default)
    {
        Platform.EnsureWindows();

        var baseKey = RegistryKey.OpenBaseKey(hive, view);
        var normalized = Normalize(keyPath);

        if (normalized.Length == 0)
            return new RegistryConfigService(baseKey);

        var key = baseKey.CreateSubKey(normalized, writable: true)
            ?? throw new InvalidOperationException($"Failed to open or create registry key: {hive}\\{normalized}");

        baseKey.Dispose();
        return new RegistryConfigService(key);
    }

    public IRegistryConfig? Open(RegistryHive hive, string keyPath, bool writable = true, RegistryView view = RegistryView.Default)
    {
        Platform.EnsureWindows();

        var baseKey = RegistryKey.OpenBaseKey(hive, view);
        var normalized = Normalize(keyPath);

        if (normalized.Length == 0)
            return new RegistryConfigService(baseKey);

        var key = baseKey.OpenSubKey(normalized, writable);
        baseKey.Dispose();
        return key is null ? null : new RegistryConfigService(key);
    }

    public IRegistryConfig OpenOrCreate(string fullPath, RegistryView view = RegistryView.Default)
    {
        var (hive, subKey) = ParseFullPath(fullPath);
        return OpenOrCreate(hive, subKey, view);
    }

    public IRegistryConfig? Open(string fullPath, bool writable = true, RegistryView view = RegistryView.Default)
    {
        var (hive, subKey) = ParseFullPath(fullPath);
        return Open(hive, subKey, writable, view);
    }

    private static string Normalize(string? keyPath)
        => string.IsNullOrWhiteSpace(keyPath) ? string.Empty : keyPath!.Replace('/', '\\').Trim('\\');

    private static (RegistryHive Hive, string SubKey) ParseFullPath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
            throw new ArgumentException("Registry path cannot be empty.", nameof(fullPath));

        var normalized = fullPath.Replace('/', '\\').TrimStart('\\');
        var separatorIndex = normalized.IndexOf('\\');
        var hiveToken = separatorIndex < 0 ? normalized : normalized.Substring(0, separatorIndex);
        var subKey = separatorIndex < 0 ? string.Empty : normalized.Substring(separatorIndex + 1);

        var hive = hiveToken.ToUpperInvariant() switch
        {
            "HKCU" or "HKEY_CURRENT_USER"   => RegistryHive.CurrentUser,
            "HKLM" or "HKEY_LOCAL_MACHINE"  => RegistryHive.LocalMachine,
            "HKCR" or "HKEY_CLASSES_ROOT"   => RegistryHive.ClassesRoot,
            "HKU"  or "HKEY_USERS"          => RegistryHive.Users,
            "HKCC" or "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => throw new ArgumentException(
                $"Unknown registry hive '{hiveToken}'. Expected HKCU, HKLM, HKCR, HKU, or HKCC (or the HKEY_* long form).",
                nameof(fullPath))
        };

        return (hive, subKey);
    }
}
