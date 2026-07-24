using Fenestra.Windows.Services;
using Microsoft.Win32;

namespace Fenestra.Windows.Tests.Utils;

internal sealed class TempRegistry : IDisposable
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

    public TempRegistry(EnumStorageMode enumStorage, params IRegistryValueConverter[] converters)
    {
        _path = $@"SOFTWARE\FenestraTests\{Guid.NewGuid():N}";
        var options = new RegistryConfigOptions { EnumStorage = enumStorage };
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