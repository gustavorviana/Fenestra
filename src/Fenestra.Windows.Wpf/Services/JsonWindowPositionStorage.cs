using Fenestra.Core;
using Fenestra.Core.Models;
using System.IO;
using System.Text.Json;

namespace Fenestra.Wpf.Services;

internal class JsonWindowPositionStorage : IWindowPositionStorage
{
    private readonly string _directory;

    public JsonWindowPositionStorage(string directory)
    {
        _directory = directory;
    }

    public WindowPositionData? Load(string key)
    {
        var path = GetPath(key);
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<WindowPositionData>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save(string key, WindowPositionData data)
    {
        try
        {
            Directory.CreateDirectory(_directory);
            var path = GetPath(key);
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Best effort
        }
    }

    public void Delete(string key)
    {
        try
        {
            var path = GetPath(key);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best effort
        }
    }

    private string GetPath(string key)
    {
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_directory, $"{safeKey}.json");
    }
}
