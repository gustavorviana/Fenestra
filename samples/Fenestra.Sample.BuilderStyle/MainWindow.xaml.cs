using System.Windows;
using Fenestra.Core;
using Fenestra.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Fenestra.Sample.BuilderStyle;

public partial class MainWindow : Window, IMinimizeToTray
{
    private readonly IDialogService _dialogs;
    private readonly ITaskbarService _taskbar;
    private readonly ITrayIconService _tray;

    public MainWindow(IDialogService dialogs, ITaskbarService taskbar, ITrayIconService tray)
    {
        InitializeComponent();
        _dialogs = dialogs;
        _taskbar = taskbar;
        _tray = tray;

        _tray.SetTooltip("Fenestra Sample");
        _tray.Click += (_, _) =>
        {
            WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState.Minimized;
            if (WindowState != WindowState.Minimized) Activate();
        };
        _tray.SetContextMenu(new[]
        {
            new TrayMenuItem("Show Window", () => { WindowState = WindowState.Normal; Activate(); })
                { Foreground = "#2196F3" },
            new TrayMenuItem("Hide Window", () => WindowState = WindowState.Minimized)
                { Foreground = "#FF9800" },
            TrayMenuItem.Separator(),
            new TrayMenuItem("Exit", () => Application.Current.Shutdown())
                { Foreground = "#F44336" }
        });
    }

    private void OnShowTray(object sender, RoutedEventArgs e)
    {
        _tray.Show();
        StatusText.Text = "Tray icon visible";
    }

    private void OnHideTray(object sender, RoutedEventArgs e)
    {
        _tray.Hide();
        StatusText.Text = "Tray icon hidden";
    }

    private void OnShowBalloon(object sender, RoutedEventArgs e)
    {
        _tray.Show();
        _tray.ShowBalloonTip("Fenestra", "Hello from the system tray!", TrayBalloonIcon.Info);
    }

    private void OnProgressChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _taskbar.SetProgress(e.NewValue / 100.0);
    }

    private void OnProgressNormal(object sender, RoutedEventArgs e)
    {
        _taskbar.SetProgressState(TaskbarProgressState.Normal);
        _taskbar.SetProgress(ProgressSlider.Value / 100.0);
    }

    private void OnProgressPaused(object sender, RoutedEventArgs e)
        => _taskbar.SetProgressState(TaskbarProgressState.Paused);

    private void OnProgressError(object sender, RoutedEventArgs e)
        => _taskbar.SetProgressState(TaskbarProgressState.Error);

    private void OnProgressIndeterminate(object sender, RoutedEventArgs e)
        => _taskbar.SetProgressState(TaskbarProgressState.Indeterminate);

    private void OnProgressClear(object sender, RoutedEventArgs e)
    {
        _taskbar.ClearProgress();
        ProgressSlider.Value = 0;
    }

    private void OnOpenFile(object sender, RoutedEventArgs e)
    {
        var result = _dialogs.OpenFileDialog(new OpenFileDialogOptions
        {
            Title = "Select a file",
            Extensions = new[]
            {
                new FileExtensionInfo("txt", "Text Files"),
                new FileExtensionInfo("*", "All Files")
            }
        });
        StatusText.Text = result != null ? $"Opened: {result}" : "Cancelled";
    }

    private void OnSaveFile(object sender, RoutedEventArgs e)
    {
        var result = _dialogs.SaveFileDialog(new SaveFileDialogOptions
        {
            Title = "Save file",
            DefaultExtension = "txt",
            FileName = "document"
        });
        StatusText.Text = result != null ? $"Save to: {result}" : "Cancelled";
    }

    private void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        var result = _dialogs.OpenFolderDialog(new FolderDialogOptions
        {
            Title = "Select a folder"
        });
        StatusText.Text = result != null ? $"Folder: {result}" : "Cancelled";
    }

    private void OnShowMessage(object sender, RoutedEventArgs e)
    {
        var result = _dialogs.ShowMessage(
            "This is a test message from Fenestra!",
            "Fenestra Sample",
            FenestraMessageButton.YesNo,
            FenestraMessageIcon.Question);
        StatusText.Text = $"Message result: {result}";
    }
}
