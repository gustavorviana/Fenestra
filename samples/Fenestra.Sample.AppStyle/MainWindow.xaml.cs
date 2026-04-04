using Fenestra.Core;
using Fenestra.Core.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Fenestra.Sample.AppStyle;

public partial class MainWindow : Window, IMinimizeToTray, IRememberWindowState
{
    private readonly ITrayIconService _tray;
    private readonly ITaskbarProvider _taskbar;
    private readonly IDialogService _dialogs;
    private readonly IEventBus _bus;
    private readonly IAutoStartService _autoStart;
    private readonly IToastService _toast;
    private readonly ObservableCollection<string> _eventLog = new();

    private IToastHandle? _progressHandle;
    private ToastProgressTracker? _progressTracker;

    public MainWindow(
        ITrayIconService tray,
        ITaskbarProvider taskbar,
        IDialogService dialogs,
        IEventBus bus,
        IAutoStartService autoStart,
        IToastService toast)
    {
        InitializeComponent();
        _tray = tray;
        _taskbar = taskbar;
        _dialogs = dialogs;
        _bus = bus;
        _autoStart = autoStart;
        _toast = toast;

        EventLogList.ItemsSource = _eventLog;
    }

    // --- General tab ---

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

    // --- Toast tab ---

    private void OnShowToast(object sender, RoutedEventArgs e)
    {
        var handle = _toast.Show(t =>
        {
            t.Title(ToastTitle.Text).Body(ToastBody.Text);
            if (!string.IsNullOrWhiteSpace(ToastAttribution.Text))
                t.Attribution(ToastAttribution.Text);
        });
        ListenToast(handle);
        SetStatus($"Toast shown (tag: {handle.Tag})");
    }

    private void OnShowToastWithScenario(object sender, RoutedEventArgs e)
    {
        var scenario = (ToastScenario)ToastScenarioCombo.SelectedIndex;
        var duration = (ToastDuration)ToastDurationCombo.SelectedIndex;

        var handle = _toast.Show(t =>
        {
            t.Title(ToastTitle.Text).Body(ToastBody.Text)
             .Scenario(scenario).Duration(duration);

            if (scenario == ToastScenario.Reminder || scenario == ToastScenario.Alarm)
                t.AddDismissButton();
        });
        ListenToast(handle);
        SetStatus($"Toast shown with scenario: {scenario}");
    }

    private void OnShowToastWithButtons(object sender, RoutedEventArgs e)
    {
        var handle = _toast.Show(t => t
            .Title("Action Required")
            .Body("Do you want to proceed?")
            .Launch("action=default")
            .EnableButtonStyles()
            .AddButton("Accept", "accept", ToastButtonStyle.Success)
            .AddButton("Decline", "decline", ToastButtonStyle.Critical)
            .AddContextMenuItem("Copy", "copy")
        );
        ListenToast(handle);
        SetStatus("Toast with buttons shown");
    }

    private void OnShowToastWithInput(object sender, RoutedEventArgs e)
    {
        var handle = _toast.Show(t => t
            .Title("Quick Reply")
            .Body("Type your response:")
            .AddTextInput("reply", placeholder: "Type here...", title: "Reply")
            .AddButton(b => b
                .Text("Send")
                .Argument("send")
                .ForInput("reply"))
        );
        ListenToast(handle);
        SetStatus("Toast with text input shown");
    }

    private void OnShowToastWithDropdown(object sender, RoutedEventArgs e)
    {
        var handle = _toast.Show(t => t
            .Title("Snooze Reminder")
            .Body("Meeting in 5 minutes")
            .Scenario(ToastScenario.Reminder)
            .AddSelectionInput("snooze", new Dictionary<string, string>
            {
                { "5", "5 minutes" },
                { "15", "15 minutes" },
                { "60", "1 hour" }
            }, title: "Snooze for:", defaultValue: "5")
            .AddSnoozeButton("snooze")
            .AddDismissButton()
        );
        ListenToast(handle);
        SetStatus("Toast with dropdown shown");
    }

    private void OnShowToastWithHero(object sender, RoutedEventArgs e)
    {
        var path = ToastImagePath.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Enter an image path first");
            return;
        }

        var handle = _toast.Show(t => t
            .Title("Hero Image Toast")
            .Body("Check out this image!")
            .HeroImage(path)
        );
        ListenToast(handle);
        SetStatus("Toast with hero image shown");
    }

    private void OnShowToastWithLogo(object sender, RoutedEventArgs e)
    {
        var path = ToastImagePath.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Enter an image path first");
            return;
        }

        var handle = _toast.Show(t => t
            .Title("App Logo Toast")
            .Body("With custom logo!")
            .AppLogo(path, ToastImageCrop.Circle)
        );
        ListenToast(handle);
        SetStatus("Toast with app logo shown");
    }

    private void OnShowToastWithAudio(object sender, RoutedEventArgs e)
    {
        var sound = ToastAudioCombo.SelectedIndex switch
        {
            1 => ToastAudio.IM,
            2 => ToastAudio.Mail,
            3 => ToastAudio.Reminder,
            4 => ToastAudio.Alarm1,
            _ => ToastAudio.Default
        };

        var handle = _toast.Show(t =>
        {
            t.Title(ToastTitle.Text).Body(ToastBody.Text);

            if (ToastSilentCheck.IsChecked == true)
                t.Silent();
            else
                t.Audio(sound);

            if (sound >= ToastAudio.Alarm1)
                t.Duration(ToastDuration.Long).AudioLoop();
        });
        ListenToast(handle);
        SetStatus($"Toast with audio: {sound}");
    }

    private void OnShowProgressToast(object sender, RoutedEventArgs e)
    {
        _progressHandle?.Dispose();

        _progressTracker = new ToastProgressTracker(title: "Downloading file.zip", useValueOverride: true);
        _progressHandle = _toast.Show(t => t
            .Title("Download")
            .BindProgress(_progressTracker)
        );
        ListenToast(_progressHandle);

        var value = ToastProgressSlider.Value / 100.0;
        _progressTracker.Report(value, ToastProgressStatus.Text);
        SetStatus("Progress toast shown — use slider to update");
    }

    private void OnToastProgressChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var pct = (int)e.NewValue;
        ToastProgressLabel.Text = $"{pct}%";

        _progressTracker?.Report(pct / 100.0, $"{ToastProgressStatus.Text} ({pct}%)", $"{pct}%");
    }

    private void OnCompleteProgressToast(object sender, RoutedEventArgs e)
    {
        _progressHandle?.Dispose();
        _progressHandle = null;
        _progressTracker = null;
        ToastProgressSlider.Value = 0;
        SetStatus("Progress toast removed");
    }

    private void OnClearToastHistory(object sender, RoutedEventArgs e)
    {
        _toast.ClearHistory();
        _progressHandle = null;
        _progressTracker = null;
        SetStatus("All notifications cleared");
    }

    // --- Event Log tab ---

    private void ListenToast(IToastHandle handle)
    {
        var tag = handle.Tag.Length > 16 ? handle.Tag[..16] + ".." : handle.Tag;
        var props = new List<string>();
        if (handle.Group != null) props.Add($"group={handle.Group}");
        if (handle.SuppressPopup) props.Add("suppress=true");
        if (handle.Priority != ToastPriority.Default) props.Add($"priority={handle.Priority}");
        if (handle.ExpiresOnReboot) props.Add("expiresOnReboot=true");
        if (handle.ExpirationTime.HasValue) props.Add($"expires={handle.ExpirationTime.Value:HH:mm:ss}");

        var extra = props.Count > 0 ? "  " + string.Join("  ", props) : "";
        LogToEvent($"Shown  tag={tag}{extra}");

        handle.Activated += (_, args) =>
        {
            var parts = new List<string> { $"Activated  tag={tag}" };

            if (!string.IsNullOrEmpty(args.Arguments))
                parts.Add($"args={args.Arguments}");
            else
                parts.Add("args=(body click)");

            foreach (var kv in args.UserInput)
                parts.Add($"{kv.Key}=\"{kv.Value}\"");

            LogToEvent(string.Join("  ", parts));
            SetStatus($"Activated: {args.Arguments}");
        };

        handle.Dismissed += (_, reason) =>
        {
            LogToEvent($"Dismissed  tag={tag}  reason={reason}");
            SetStatus($"Dismissed: {reason}");
        };

        handle.Failed += (_, errorCode) =>
        {
            LogToEvent($"Failed  tag={tag}  HRESULT=0x{errorCode:X8}");
            SetStatus($"Failed: 0x{errorCode:X8}");
        };
    }

    private void LogToEvent(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _eventLog.Add(line);

        // Auto-scroll to latest
        if (_eventLog.Count > 0)
            EventLogList.ScrollIntoView(_eventLog[_eventLog.Count - 1]);
    }

    private void OnClearEventLog(object sender, RoutedEventArgs e)
    {
        _eventLog.Clear();
    }

    private void SetStatus(string text) => StatusText.Text = text;
}
