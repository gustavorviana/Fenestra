using System.Windows;
using System.Windows.Shell;
using Fenestra.Core;
using Fenestra.Core.Models;

namespace Fenestra.Wpf.Services;

internal class TaskbarService : ITaskbarService
{
    public void SetProgress(double value)
    {
        InvokeOnMainWindow(info =>
        {
            info.ProgressState = TaskbarItemProgressState.Normal;
            info.ProgressValue = value;
        });
    }

    public void SetProgressState(TaskbarProgressState state)
    {
        InvokeOnMainWindow(info =>
        {
            info.ProgressState = ToWpfState(state);
        });
    }

    public void ClearProgress()
    {
        InvokeOnMainWindow(info =>
        {
            info.ProgressState = TaskbarItemProgressState.None;
            info.ProgressValue = 0;
        });
    }

    private static void InvokeOnMainWindow(Action<TaskbarItemInfo> action)
    {
        var app = Application.Current;
        if (app == null) return;

        app.Dispatcher.Invoke(() =>
        {
            var mainWindow = app.MainWindow;
            if (mainWindow == null) return;

            mainWindow.TaskbarItemInfo ??= new TaskbarItemInfo();
            action(mainWindow.TaskbarItemInfo);
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
