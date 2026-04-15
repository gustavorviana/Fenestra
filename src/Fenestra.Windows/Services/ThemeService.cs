using Fenestra.Core;
using Fenestra.Core.Models;
using Microsoft.Win32;

namespace Fenestra.Windows.Services;

/// <summary>
/// Monitors the Windows app theme (dark/light) and raises events on change.
/// When mode is Dark or Light, registry monitoring is disabled and the value is fixed.
/// </summary>
internal class ThemeService : FenestraComponent, IThemeService
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ValueName = "AppsUseLightTheme";

    private readonly IThreadContext _threadContext;
    private RegistryWatcher? _watcher;
    private AppThemeMode _mode;
    private bool _isDarkMode;

    /// <inheritdoc />
    public AppThemeMode Mode => _mode;

    /// <inheritdoc />
    public bool IsDarkMode => _isDarkMode;

    /// <inheritdoc />
    public event BusHandler<bool>? ThemeChanged;

    public ThemeService(IThreadContext threadContext)
    {
        Platform.EnsureWindows10();
        _threadContext = threadContext;
        SetMode(AppThemeMode.System);
    }

    /// <inheritdoc />
    public void SetMode(AppThemeMode mode)
    {
        StopWatching();
        _mode = mode;

        switch (mode)
        {
            case AppThemeMode.Dark:
                UpdateDarkMode(true);
                break;
            case AppThemeMode.Light:
                UpdateDarkMode(false);
                break;
            case AppThemeMode.System:
            default:
                UpdateDarkMode(ReadFromRegistry());
                StartWatching();
                break;
        }
    }

    private void UpdateDarkMode(bool isDark)
    {
        var changed = isDark != _isDarkMode;
        _isDarkMode = isDark;

        if (changed)
            _ = _threadContext.InvokeAsync(() => ThemeChanged?.Invoke(_isDarkMode));
    }

    private void StartWatching()
    {
        _watcher = new RegistryWatcher(PersonalizeKey, () =>
        {
            var newValue = ReadFromRegistry();
            if (newValue != _isDarkMode)
                UpdateDarkMode(newValue);
        });
        _watcher.Start();
    }

    private void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
    }

    private static bool ReadFromRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey, false);
            var value = key?.GetValue(ValueName);
            if (value is int i) return i == 0;
        }
        catch { }
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        StopWatching();
    }
}
