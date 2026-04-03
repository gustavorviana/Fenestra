using Fenestra.Core;
using Fenestra.Core.Models;
using System.Windows;

namespace Fenestra.Sample.AppStyle;

public partial class MainWindow : Window, IMinimizeToTray, IRememberWindowState
{
    private readonly ITrayIconService _tray;
    private readonly ITaskbarProvider _taskbar;
    private readonly IDialogService _dialogs;
    private readonly IEventBus _bus;
    private readonly IAutoStartService _autoStart;

    public MainWindow(
        ITrayIconService tray,
        ITaskbarProvider taskbar,
        IDialogService dialogs,
        IEventBus bus,
        IAutoStartService autoStart)
    {
        InitializeComponent();
        _tray = tray;
        _taskbar = taskbar;
        _dialogs = dialogs;
        _bus = bus;
        _autoStart = autoStart;
    }

    private void OnShowTray(object sender, RoutedEventArgs e)
    {
        _tray.Show();
        SetStatus("Tray icon visible");
    }

    private void OnShowBalloon(object sender, RoutedEventArgs e)
    {
        _bus.PublishAsync(new BalloonRequestEvent("Fenestra", "Hello from the sample app!"));
    }

    private async void OnSimulateProgress(object sender, RoutedEventArgs e)
    {
        using var progress = _taskbar.Create(this);

        for (int i = 0; i <= 100; i += 5)
        {
            progress.SetProgress(i / 100.0);
            SetStatus($"Progress: {i}%");
            await Task.Delay(100);
        }

        progress.SetState(TaskbarProgressState.Paused);
        SetStatus("Progress complete");
    }

    private void OnOpenFile(object sender, RoutedEventArgs e)
    {
        var result = _dialogs.OpenFileDialog(new OpenFileDialogOptions
        {
            Title = "Select a file",
            Extensions = [new FileExtensionInfo("txt", "Text Files"), new FileExtensionInfo("*", "All Files")]
        });
        SetStatus(result != null ? $"Opened: {result}" : "Cancelled");
    }

    private void OnSaveFile(object sender, RoutedEventArgs e)
    {
        var result = _dialogs.SaveFileDialog(new SaveFileDialogOptions
        {
            Title = "Save file",
            DefaultExtension = "txt",
            FileName = "document",
            Extensions = [new FileExtensionInfo("txt", "Text Files"), new FileExtensionInfo("*", "All Files")]
        });
        SetStatus(result != null ? $"Save to: {result}" : "Cancelled");
    }

    private void OnShowMessage(object sender, RoutedEventArgs e)
    {
        var result = _dialogs.ShowMessage(
            "This is a sample message from Fenestra!",
            "Fenestra Sample",
            FenestraMessageButton.YesNo,
            FenestraMessageIcon.Question);
        SetStatus($"Message result: {result}");
    }

    private void OnCheckAutoStart(object sender, RoutedEventArgs e)
    {
        SetStatus($"Auto-start — Enabled: {_autoStart.IsEnabled}, Initialized: {_autoStart.IsInitialized()}");
    }

    private void SetStatus(string text) => StatusText.Text = text;
}
