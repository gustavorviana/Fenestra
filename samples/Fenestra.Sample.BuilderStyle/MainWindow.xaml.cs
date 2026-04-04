using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using System.Windows;

namespace Fenestra.Sample.BuilderStyle;

public partial class MainWindow : Window, IMinimizeToTray
{
    private readonly IDialogService _dialogs;
    private readonly ITaskbarProvider _taskbar;
    private readonly ITrayIconService _tray;
    private ITaskbarProgress? _progress;

    public MainWindow(IDialogService dialogs, ITaskbarProvider taskbar, ITrayIconService tray)
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

    private ITaskbarProgress EnsureProgress() => _progress ??= _taskbar.Create(this);

    private void OnProgressChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        EnsureProgress().SetProgress(e.NewValue / 100.0);
    }

    private void OnProgressNormal(object sender, RoutedEventArgs e)
    {
        var p = EnsureProgress();
        p.SetState(TaskbarProgressState.Normal);
        p.SetProgress(ProgressSlider.Value / 100.0);
    }

    private void OnProgressPaused(object sender, RoutedEventArgs e)
        => EnsureProgress().SetState(TaskbarProgressState.Paused);

    private void OnProgressError(object sender, RoutedEventArgs e)
        => EnsureProgress().SetState(TaskbarProgressState.Error);

    private void OnProgressIndeterminate(object sender, RoutedEventArgs e)
        => EnsureProgress().SetState(TaskbarProgressState.Indeterminate);

    private void OnProgressClear(object sender, RoutedEventArgs e)
    {
        _progress?.Dispose();
        _progress = null;
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
