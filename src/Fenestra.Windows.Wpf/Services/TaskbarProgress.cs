using Fenestra.Windows;
using Fenestra.Windows.Models;
using System.Windows;
using System.Windows.Shell;

namespace Fenestra.Wpf.Services;

internal class TaskbarProgress : ITaskbarProgress
{
    private TaskbarItemProgressState _state = TaskbarItemProgressState.Normal;
    private readonly TaskbarProvider _owner;
    private readonly Window _window;
    private bool _disposed;

    internal bool IsDisposed => _disposed;

    internal TaskbarProgress(Window window, TaskbarProvider owner)
    {
        _window = window;
        _owner = owner;
    }

    public void SetProgress(double value)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TaskbarProgress));

        Invoke(info =>
        {
            info.ProgressState = _state;
            info.ProgressValue = value;
        });
    }

    public void SetState(TaskbarProgressState state)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TaskbarProgress));

        Invoke(info =>
        {
            _state = ToWpfState(state);
            info.ProgressState = _state;
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Invoke(info =>
        {
            info.ProgressState = TaskbarItemProgressState.None;
            info.ProgressValue = 0;
        });

        _owner.OnDisposed(this, _window);
    }

    private void Invoke(Action<TaskbarItemInfo> action)
    {
        _window.Dispatcher.Invoke(() =>
        {
            _window.TaskbarItemInfo ??= new TaskbarItemInfo();
            action(_window.TaskbarItemInfo);
        });
    }

    private static TaskbarItemProgressState ToWpfState(TaskbarProgressState state) => state switch
    {
        TaskbarProgressState.None => TaskbarItemProgressState.None,
        TaskbarProgressState.Indeterminate => TaskbarItemProgressState.Indeterminate,
        TaskbarProgressState.Normal => TaskbarItemProgressState.Normal,
        TaskbarProgressState.Paused => TaskbarItemProgressState.Paused,
        TaskbarProgressState.Error => TaskbarItemProgressState.Error,
        _ => TaskbarItemProgressState.None
    };
}
