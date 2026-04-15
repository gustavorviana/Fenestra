using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows;
using Fenestra.Windows.Localization;
using Fenestra.Windows.Models;
using Fenestra.Wpf;
using System.Globalization;
using System.Resources;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fenestra.Sample.BuilderStyle;

public partial class MainWindow : Window, IMinimizeToTray
{
    private readonly IDialogService _dialogs;
    private readonly ITaskbarProvider _taskbar;
    private readonly ITrayIconService _tray;
    private readonly ICredentialVault _vault;
    private readonly IIdleDetectionService _idle;
    private readonly IAppLifecycleService _lifecycle;
    private readonly ILocalizationService _localization;
    private readonly IJumpListService _jumpList;
    private readonly ITaskbarOverlayService _overlay;
    private readonly DispatcherTimer _idleUiTimer;
    private ITaskbarProgress? _progress;

    public MainWindow(
        IDialogService dialogs,
        ITaskbarProvider taskbar,
        ITrayIconService tray,
        ICredentialVault vault,
        IIdleDetectionService idle,
        IAppLifecycleService lifecycle,
        ILocalizationService localization,
        IJumpListService jumpList,
        ITaskbarOverlayService overlay)
    {
        InitializeComponent();
        _dialogs = dialogs;
        _taskbar = taskbar;
        _tray = tray;
        _vault = vault;
        _idle = idle;
        _lifecycle = lifecycle;
        _localization = localization;
        _jumpList = jumpList;
        _overlay = overlay;

        // Idle detection — wire events and a local timer to refresh the "Idle time" text.
        _idle.BecameIdle += (_, _) => IdleStatusText.Text = "Status: IDLE (no input for 10s)";
        _idle.BecameActive += (_, _) => IdleStatusText.Text = "Status: active";
        _idleUiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _idleUiTimer.Tick += (_, _) => IdleTimeText.Text = $"Idle time: {_idle.IdleTime.TotalSeconds:F0}s";
        _idleUiTimer.Start();

        // App lifecycle — populate the labels once at startup (snapshot semantics).
        LifecycleFirstRunText.Text = $"First run ever: {(_lifecycle.IsFirstRun ? "YES" : "no")}";
        LifecycleVersionText.Text = $"First run of version: {(_lifecycle.IsFirstRunOfVersion ? "YES" : "no")}";
        LifecyclePrevVersionText.Text = $"Previous version: {_lifecycle.PreviousVersion?.ToString() ?? "(none)"}";
        LifecycleInstallDateText.Text = $"First install: {_lifecycle.FirstInstallDate:yyyy-MM-dd HH:mm:ss zzz}";
        LifecycleLaunchCountText.Text = $"Launch count: {_lifecycle.LaunchCount}";

        // Bridge: when the localization service changes culture, tell the translation
        // source to invalidate its bindings so WPF re-evaluates every {fenestra:Tr ...}.
        _localization.CultureChanged += (_, _) =>
        {
            TranslationSource.Instance.Invalidate();
            Dispatcher.Invoke(UpdateLocalizationDisplay);
        };
        UpdateLocalizationDisplay();

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

    // --- Credential Vault ---

    private const string StringKey = "sample-api-token";
    private const string BytesKey = "sample-binary-key";

    private void OnCredStoreString(object sender, RoutedEventArgs e)
    {
        _vault.Store(StringKey, "alice", "ghp_sample_token_xyz123");
        StatusText.Text = $"Stored string credential '{StringKey}'. Check Credential Manager.";
    }

    private void OnCredReadString(object sender, RoutedEventArgs e)
    {
        var cred = _vault.Read(StringKey);
        StatusText.Text = cred is null
            ? $"No credential found for '{StringKey}'. Click 'Store (string)' first."
            // ToString() masks the secret — safe to log
            : $"Read: {cred}";
    }

    private void OnCredStoreBytes(object sender, RoutedEventArgs e)
    {
        var randomKey = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        _vault.Store(BytesKey, "alice", randomKey);
        StatusText.Text = $"Stored {randomKey.Length}-byte credential '{BytesKey}'.";
    }

    private void OnCredReadBytes(object sender, RoutedEventArgs e)
    {
        using var stored = _vault.ReadBytes(BytesKey);
        if (stored is null)
        {
            StatusText.Text = $"No credential found for '{BytesKey}'. Click 'Store (bytes)' first.";
            return;
        }
        // Use stored.Secret inside the using scope; on Dispose it gets zero-filled.
        StatusText.Text = $"Read {stored.Secret.Length} bytes from '{BytesKey}' (buffer will be zeroed on scope exit).";
    }

    private void OnCredList(object sender, RoutedEventArgs e)
    {
        var targets = _vault.Enumerate();
        StatusText.Text = targets.Count == 0
            ? "No credentials stored by this app yet."
            : $"Credentials: {string.Join(", ", targets)}";
    }

    private void OnCredDeleteAll(object sender, RoutedEventArgs e)
    {
        _vault.Delete(StringKey);
        _vault.Delete(BytesKey);
        StatusText.Text = "Deleted all sample credentials.";
    }

    // --- Localization ---

    private void UpdateLocalizationDisplay()
    {
        // Static translated strings live in XAML via {fenestra:Tr messages, Key} and
        // refresh automatically when the culture changes — no code needed here.
        //
        // Only the dynamic parts (culture name + formatted date/number) are set from
        // code-behind, because they combine translated labels with runtime values.
        var c = _localization.CurrentCulture;
        var languageLabel = TranslationSource.Instance["messages", "LanguageLabel"];

        LocCurrentText.Text = $"{languageLabel}: {c.Name} ({c.NativeName})";
        LocDateText.Text = $"Date: {DateTime.Now.ToString("D", c)}";
        LocNumberText.Text = $"Number: {1234567.89.ToString("N", c)}";
    }

    private void OnSetEnUs(object sender, RoutedEventArgs e)
        => _localization.SetCulture(CultureInfo.GetCultureInfo("en-US"));

    private void OnSetPtBr(object sender, RoutedEventArgs e)
        => _localization.SetCulture(CultureInfo.GetCultureInfo("pt-BR"));

    private void OnSetEsEs(object sender, RoutedEventArgs e)
        => _localization.SetCulture(CultureInfo.GetCultureInfo("es-ES"));

    // --- Jump List ---

    private void OnJumpSetTasks(object sender, RoutedEventArgs e)
    {
        // User tasks always appear at the bottom of the Jump List under "Tasks".
        // ApplicationPath is auto-filled with the current .exe when left null.
        _jumpList.SetTasks(
            new JumpListTask
            {
                Title = "New Window",
                Description = "Open a fresh instance with no state",
                Arguments = "--new-window",
            },
            new JumpListTask
            {
                Title = "Show Help",
                Description = "Open the Fenestra docs",
                Arguments = "--help",
            });
        StatusText.Text = "Tasks buffered. Click 'Apply' to commit.";
    }

    private void OnJumpApply(object sender, RoutedEventArgs e)
    {
        try
        {
            _jumpList.Apply();
            StatusText.Text = "Jump List applied. Right-click the taskbar icon to see it.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Jump List apply failed: {ex.Message}";
        }
    }

    // --- Taskbar Overlay ---

    private void OnOverlayBadge3(object sender, RoutedEventArgs e)
    {
        // SetBadgeOverlay is a WPF extension that renders a circular badge in code — no .ico needed.
        _overlay.SetBadgeOverlay("3", Colors.Crimson, Colors.White, "3 unread items");
        StatusText.Text = "Overlay set: red badge '3'. Check the taskbar icon.";
    }

    private void OnOverlayWarning(object sender, RoutedEventArgs e)
    {
        _overlay.SetBadgeOverlay("!", Colors.Goldenrod, Colors.Black, "Warning");
        StatusText.Text = "Overlay set: yellow warning '!'.";
    }

    private void OnOverlayClear(object sender, RoutedEventArgs e)
    {
        _overlay.Clear();
        StatusText.Text = "Taskbar overlay cleared.";
    }
}
