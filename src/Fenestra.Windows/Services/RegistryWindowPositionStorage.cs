using Fenestra.Core;
using Fenestra.Core.Models;

namespace Fenestra.Windows.Services;

/// <summary>
/// Stores window position data in the Windows Registry via <see cref="IRegistryConfig"/>.
/// Each window key becomes a subkey under "WindowState".
/// </summary>
internal sealed class RegistryWindowPositionStorage : IWindowPositionStorage
{
    private const string SectionName = "WindowState";
    private readonly IRegistryConfig _config;

    public RegistryWindowPositionStorage(IRegistryConfig config)
    {
        _config = config;
    }

    public WindowPositionData? Load(string key)
    {
        using var section = _config.GetSection(SectionName);
        if (section is null) return null;

        using var entry = section.GetSection(SanitizeKey(key));
        if (entry is null) return null;

        var left = entry.Get<double>("Left");
        var top = entry.Get<double>("Top");
        var width = entry.Get<double>("Width");
        var height = entry.Get<double>("Height");
        var state = entry.Get<int>("State");

        if (width == 0 && height == 0) return null;

        return new WindowPositionData
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            State = state
        };
    }

    public void Save(string key, WindowPositionData data)
    {
        using var section = _config.GetSection(SectionName, createIfNotExists: true)!;
        using var entry = section.GetSection(SanitizeKey(key), createIfNotExists: true)!;

        entry.Set("Left", data.Left);
        entry.Set("Top", data.Top);
        entry.Set("Width", data.Width);
        entry.Set("Height", data.Height);
        entry.Set("State", data.State);
    }

    public void Delete(string key)
    {
        using var section = _config.GetSection(SectionName);
        section?.DeleteSection(SanitizeKey(key));
    }

    private static string SanitizeKey(string key)
    {
        return key.Replace('.', '_').Replace('+', '_');
    }
}
