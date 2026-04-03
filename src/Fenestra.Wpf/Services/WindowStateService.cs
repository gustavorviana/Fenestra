using System.IO;
using System.Text.Json;
using System.Windows;
using Fenestra.Core;
using Fenestra.Core.Models;

namespace Fenestra.Wpf.Services;

internal class WindowStateService
{
    private readonly string _stateFolder;
    private readonly Dictionary<string, WindowStateData> _cache = new();

    public WindowStateService(AppInfo appInfo)
    {
        var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        _stateFolder = Path.Combine(appData, appInfo.AppName, "WindowState");
    }

    public void Attach(Window window)
    {
        if (window is not IRememberWindowState) return;

        var key = window.GetType().FullName ?? window.GetType().Name;
        var state = Load(key);

        if (state != null)
        {
            window.Left = state.Left;
            window.Top = state.Top;
            window.Width = state.Width;
            window.Height = state.Height;
            window.WindowState = (WindowState)state.State;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
        }

        window.Closing += (_, _) => Save(key, window);
    }

    private void Save(string key, Window window)
    {
        var state = window.WindowState == WindowState.Minimized
            ? WindowState.Normal
            : window.WindowState;

        var data = new WindowStateData
        {
            Left = window.RestoreBounds.Left,
            Top = window.RestoreBounds.Top,
            Width = window.RestoreBounds.Width,
            Height = window.RestoreBounds.Height,
            State = (int)state
        };

        _cache[key] = data;

        try
        {
            Directory.CreateDirectory(_stateFolder);
            var path = GetPath(key);
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Best effort - don't crash on state persistence failure
        }
    }

    private WindowStateData? Load(string key)
    {
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var path = GetPath(key);
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<WindowStateData>(json);
            if (data != null) _cache[key] = data;
            return data;
        }
        catch
        {
            return null;
        }
    }

    private string GetPath(string key)
    {
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_stateFolder, $"{safeKey}.json");
    }

    private class WindowStateData
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int State { get; set; }
    }
}
