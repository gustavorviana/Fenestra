using Fenestra.Core;
using Fenestra.Core.Exceptions;
using System.Windows;

namespace Fenestra.Wpf.Services;

internal class WindowManager : IWindowManager
{
    private readonly IServiceProvider _services;
    private readonly WindowStateService _windowState;
    private readonly MinimizeToTrayService? _minimizeToTray;
    private readonly Dictionary<Type, Window> _openWindows = new();

    public WindowManager(IServiceProvider services, WindowStateService windowState)
    {
        _services = services;
        _windowState = windowState;
        _minimizeToTray = services.GetService(typeof(MinimizeToTrayService)) as MinimizeToTrayService;
    }

    public T Show<T>() where T : class
    {
        return Show<T>(null!);
    }

    public T Show<T>(object dataContext) where T : class
    {
        var windowType = typeof(T);

        if (typeof(ISingleWindow).IsAssignableFrom(windowType) && _openWindows.TryGetValue(windowType, out var existing))
        {
            BringWindowToFront(existing);
            return (T)(object)existing;
        }

        var window = ResolveWindow<T>();

        if (dataContext != null)
        {
            window.DataContext = dataContext;
        }

        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        TrackWindow(windowType, window);

        window.Show();
        return (T)(object)window;
    }

    public bool ShowDialog<T>() where T : class, IDialog
    {
        return ShowDialog<T>(null!);
    }

    public bool ShowDialog<T>(object dataContext) where T : class, IDialog
    {
        ValidateDialogType(typeof(T));

        var window = ResolveWindow<T>();

        if (dataContext != null)
        {
            window.DataContext = dataContext;
        }

        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        TrackWindow(typeof(T), window);

        var result = window.ShowDialog();
        return result == true;
    }

    public TResult ShowDialog<T, TResult>() where T : class, IDialog<TResult>
    {
        return ShowDialog<T, TResult>(null!);
    }

    public TResult ShowDialog<T, TResult>(object dataContext) where T : class, IDialog<TResult>
    {
        ValidateDialogType(typeof(T));

        var window = ResolveWindow<T>();

        if (dataContext != null)
        {
            window.DataContext = dataContext;
        }

        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        TrackWindow(typeof(T), window);

        window.ShowDialog();

        var dialog = (IDialog<TResult>)(object)window;
        return dialog.Result!;
    }

    public T? GetOpenWindow<T>() where T : class
    {
        return _openWindows.TryGetValue(typeof(T), out var window) ? (T)(object)window : null;
    }

    public void Close<T>() where T : class
    {
        if (_openWindows.TryGetValue(typeof(T), out var window))
        {
            window.Close();
        }
    }

    public void CloseAllDialogs()
    {
        var dialogs = _openWindows
            .Where(kv => typeof(IDialog).IsAssignableFrom(kv.Key))
            .Select(kv => kv.Value)
            .ToList();

        foreach (var dialog in dialogs)
        {
            dialog.Close();
        }
    }

    private Window ResolveWindow<T>() where T : class
    {
        var windowType = typeof(T);

        try
        {
            var instance = _services.GetService(windowType)
                ?? throw new LaunchWindowException(windowType);

            if (instance is not Window window)
            {
                throw new LaunchWindowException(windowType);
            }

            return window;
        }
        catch (LaunchWindowException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new LaunchWindowException(windowType, ex);
        }
    }

    private void TrackWindow(Type windowType, Window window)
    {
        _openWindows[windowType] = window;
        _windowState.Attach(window);
        _minimizeToTray?.Attach(window);
        window.Closed += (_, _) => _openWindows.Remove(windowType);
    }

    private static void BringWindowToFront(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
        window.Focus();
    }

    private static void ValidateDialogType(Type type)
    {
        if (typeof(ISingleWindow).IsAssignableFrom(type))
        {
            throw new InvalidOperationException(
                $"Type '{type.FullName}' cannot implement both ISingleWindow and IDialog.");
        }
    }
}
