# Toast Notifications

Windows toast notifications with a fluent builder API -- titles, buttons, text inputs, progress bars, images, and audio.

## Table of Contents

- [Quick Start](#quick-start)
- [Simple Toast](#simple-toast)
- [Action Buttons](#action-buttons)
- [Text Input](#text-input)
- [Progress Bar](#progress-bar)
- [Images](#images)
- [Audio](#audio)
- [API Reference: ToastBuilder](#api-reference-toastbuilder)
- [API Reference: IToastHandle](#api-reference-itoasthandle)

## Quick Start

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.Services.AddWindowsToastNotifications();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IToastService _toast;

    public MainWindow(IToastService toast)
    {
        _toast = toast;
        Title = "Toast Demo";
        Width = 400;
        Height = 200;

        var button = new System.Windows.Controls.Button { Content = "Show Toast" };
        button.Click += (_, _) =>
        {
            _toast.Show(t => t
                .Title("Hello")
                .Body("This is a toast notification."));
        };
        Content = button;
    }
}
```

## Simple Toast

A toast with title, body, and attribution text.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.Services.AddWindowsToastNotifications();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IToastService _toast;

    public MainWindow(IToastService toast)
    {
        _toast = toast;
        Title = "Simple Toast";
        Width = 400;
        Height = 200;

        var button = new System.Windows.Controls.Button { Content = "Show Toast" };
        button.Click += (_, _) =>
        {
            _toast.Show(t => t
                .Title("File Saved")
                .Body("report-2026.pdf was saved to Documents.")
                .Attribution("via MyApp")
                .Duration(ToastDuration.Short)
                .Launch("action=open&file=report-2026.pdf"));
        };
        Content = button;
    }
}
```

## Action Buttons

Buttons appear at the bottom of the toast. Use the `Activated` event on `IToastHandle` to handle clicks. The `Arguments` property tells you which button (or toast body) was clicked.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.Services.AddWindowsToastNotifications();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IToastService _toast;

    public MainWindow(IToastService toast)
    {
        _toast = toast;
        Title = "Buttons Demo";
        Width = 400;
        Height = 200;

        var button = new System.Windows.Controls.Button { Content = "Show Toast" };
        button.Click += (_, _) => ShowToastWithButtons();
        Content = button;
    }

    private void ShowToastWithButtons()
    {
        var handle = _toast.Show(t => t
            .Title("Incoming Update")
            .Body("Version 2.1 is available.")
            .EnableButtonStyles()
            .AddButton("Install Now", "action=install", ToastButtonStyle.Success)
            .AddButton("Later", "action=later", ToastButtonStyle.Critical)
            .AddDismissButton());

        handle.Activated += (_, args) =>
        {
            // args.Arguments => "action=install" or "action=later"
            if (args.Arguments == "action=install")
            {
                Dispatcher.Invoke(() => Title = "Installing...");
            }
        };

        handle.Dismissed += (_, reason) =>
        {
            // reason => UserCanceled, ApplicationHidden, or TimedOut
        };
    }
}
```

Styled buttons require `EnableButtonStyles()`. Available styles:

| Style | Appearance |
|---|---|
| `ToastButtonStyle.Default` | Standard button |
| `ToastButtonStyle.Success` | Green accent |
| `ToastButtonStyle.Critical` | Red accent |

## Text Input

Add a text box and a reply button. The user's input is available in `args.UserInput` keyed by the input id.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.Services.AddWindowsToastNotifications();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IToastService _toast;

    public MainWindow(IToastService toast)
    {
        _toast = toast;
        Title = "Text Input Demo";
        Width = 400;
        Height = 200;

        var button = new System.Windows.Controls.Button { Content = "Show Toast" };
        button.Click += (_, _) => ShowInputToast();
        Content = button;
    }

    private void ShowInputToast()
    {
        var handle = _toast.Show(t => t
            .Title("New Message from Alice")
            .Body("Hey, are you free for lunch?")
            .AddTextInput("reply", placeholder: "Type a reply...")
            .AddButton(b => b
                .Text("Send")
                .Argument("action=reply")
                .ForInput("reply")));

        handle.Activated += (_, args) =>
        {
            if (args.Arguments == "action=reply")
            {
                var replyText = args.UserInput["reply"];
                // replyText => whatever the user typed
            }
        };
    }
}
```

## Progress Bar

### Static Progress

Set a fixed progress value directly.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.Services.AddWindowsToastNotifications();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IToastService _toast;

    public MainWindow(IToastService toast)
    {
        _toast = toast;
        Title = "Static Progress";
        Width = 400;
        Height = 200;

        var button = new System.Windows.Controls.Button { Content = "Show Toast" };
        button.Click += (_, _) =>
        {
            _toast.Show(t => t
                .Title("Uploading report.pdf")
                .Progress(status: "Uploading...", value: 0.65, title: "report.pdf"));
        };
        Content = button;
    }
}
```

### Dynamic Progress with ToastProgressTracker

`ToastProgressTracker` binds to the toast and pushes live updates via data binding. Call `Report()` as work progresses.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.Services.AddWindowsToastNotifications();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IToastService _toast;

    public MainWindow(IToastService toast)
    {
        _toast = toast;
        Title = "Dynamic Progress";
        Width = 400;
        Height = 200;

        var button = new System.Windows.Controls.Button { Content = "Start Download" };
        button.Click += async (_, _) => await SimulateDownloadAsync();
        Content = button;
    }

    private async Task SimulateDownloadAsync()
    {
        var tracker = new ToastProgressTracker(title: "backup.zip", useValueOverride: true);

        _toast.Show(t => t
            .Title("Downloading...")
            .BindProgress(tracker));

        for (int i = 0; i <= 10; i++)
        {
            await Task.Delay(500);
            double progress = i / 10.0;
            tracker.Report(progress, "Downloading...", $"{i * 10}%");
            // The toast updates in real time
        }

        tracker.Report(1.0, "Complete!");
    }
}
```

`ToastProgressTracker.Report()` overloads:

| Overload | Description |
|---|---|
| `Report(double value)` | Updates progress value only (0.0 to 1.0) |
| `Report(string status)` | Updates status text only |
| `Report(double value, string status)` | Updates both value and status |
| `Report(double value, string status, string? valueOverride)` | Updates value, status, and custom text (requires `useValueOverride: true`) |

## Images

Three image placements are available: inline (full-width below text), hero (banner at top), and app logo (small icon on the left).

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.Services.AddWindowsToastNotifications();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IToastService _toast;

    public MainWindow(IToastService toast)
    {
        _toast = toast;
        Title = "Images Demo";
        Width = 400;
        Height = 300;

        var panel = new System.Windows.Controls.StackPanel();

        var heroBtn = new System.Windows.Controls.Button { Content = "Hero Image" };
        heroBtn.Click += (_, _) =>
        {
            _toast.Show(t => t
                .Title("New wallpaper available")
                .Body("A new landscape wallpaper has been added.")
                .HeroImage(@"C:\Images\landscape.png", alt: "Mountain landscape"));
        };
        panel.Children.Add(heroBtn);

        var inlineBtn = new System.Windows.Controls.Button { Content = "Inline Image" };
        inlineBtn.Click += (_, _) =>
        {
            _toast.Show(t => t
                .Title("Screenshot captured")
                .Body("Saved to Screenshots folder.")
                .InlineImage(@"C:\Screenshots\capture.png", alt: "Screenshot"));
        };
        panel.Children.Add(inlineBtn);

        var logoBtn = new System.Windows.Controls.Button { Content = "App Logo" };
        logoBtn.Click += (_, _) =>
        {
            _toast.Show(t => t
                .Title("Message from Bob")
                .Body("See you at 3pm!")
                .AppLogo(@"C:\Images\bob-avatar.png", crop: ToastImageCrop.Circle, alt: "Bob"));
        };
        panel.Children.Add(logoBtn);

        Content = panel;
    }
}
```

| Placement | Method | Description |
|---|---|---|
| Inline | `InlineImage(source, alt?)` | Full-width image below text |
| Hero | `HeroImage(source, alt?)` | Wide banner at the top of the toast |
| App Logo | `AppLogo(source, crop?, alt?)` | Small image on the left. `ToastImageCrop.Circle` for round. |

## Audio

Configure notification sounds, looping audio, custom audio files, or silence.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.Services.AddWindowsToastNotifications();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IToastService _toast;

    public MainWindow(IToastService toast)
    {
        _toast = toast;
        Title = "Audio Demo";
        Width = 400;
        Height = 300;

        var panel = new System.Windows.Controls.StackPanel();

        var reminderBtn = new System.Windows.Controls.Button { Content = "Reminder Sound" };
        reminderBtn.Click += (_, _) =>
        {
            _toast.Show(t => t
                .Title("Meeting in 5 minutes")
                .Body("Standup in Room 3.")
                .Audio(ToastAudio.Reminder));
        };
        panel.Children.Add(reminderBtn);

        var alarmBtn = new System.Windows.Controls.Button { Content = "Looping Alarm" };
        alarmBtn.Click += (_, _) =>
        {
            _toast.Show(t => t
                .Title("Timer Expired")
                .Body("Your 25-minute focus session is over.")
                .Scenario(ToastScenario.Alarm)
                .Duration(ToastDuration.Long)
                .Audio(ToastAudio.Alarm3)
                .AudioLoop()
                .AddDismissButton());
        };
        panel.Children.Add(alarmBtn);

        var silentBtn = new System.Windows.Controls.Button { Content = "Silent Toast" };
        silentBtn.Click += (_, _) =>
        {
            _toast.Show(t => t
                .Title("Sync complete")
                .Body("42 files synchronized.")
                .Silent());
        };
        panel.Children.Add(silentBtn);

        Content = panel;
    }
}
```

> **Note:** Looping audio requires `Duration(ToastDuration.Long)` and a `Scenario` that supports it (`Alarm`, `IncomingCall`).

Available `ToastAudio` values: `Default`, `IM`, `Mail`, `Reminder`, `SMS`, `Alarm1`-`Alarm10`, `Call1`-`Call10`, `Silent`.

## API Reference: ToastBuilder

| Method | Description |
|---|---|
| `Title(string)` | Sets the title (first line) |
| `Body(string)` | Sets the body text (second line) |
| `Attribution(string)` | Sets attribution text at the bottom |
| `Launch(string)` | Sets arguments passed when the toast body is clicked |
| `ActivationType(ToastActivationType)` | Sets activation type: `Foreground`, `Background`, `Protocol` |
| `Duration(ToastDuration)` | `Short` (default) or `Long` |
| `Scenario(ToastScenario)` | `Default`, `Reminder`, `Alarm`, `IncomingCall`, `Urgent` |
| `Timestamp(DateTimeOffset)` | Custom timestamp displayed in Action Center |
| `EnableButtonStyles()` | Enables colored button styles (`Success`/`Critical`) |
| `Tag(string)` | Identifies the toast for updates and removal |
| `Group(string)` | Groups toasts for bulk removal |
| `SuppressPopup()` | Sends toast directly to Action Center without popup |
| `Priority(ToastPriority)` | `Default` or `High` |
| `ExpiresOnReboot()` | Removes toast from Action Center on reboot |
| `ExpirationTime(DateTimeOffset)` | Absolute time when toast auto-expires |
| `InlineImage(source, alt?)` | Adds a full-width inline image |
| `AppLogo(source, crop?, alt?)` | Sets the small app logo image |
| `HeroImage(source, alt?)` | Sets the hero banner image |
| `Audio(ToastAudio)` | Sets a predefined system sound |
| `AudioCustom(string uri)` | Sets a custom audio file URI |
| `AudioLoop()` | Enables looping audio (requires `Duration.Long`) |
| `Silent()` | Suppresses all notification sound |
| `Header(id, title, arguments, activationType?)` | Groups notifications in Action Center under a header |
| `Progress(status, value?, title?, valueOverride?)` | Adds a static progress bar |
| `BindProgress(ToastProgressTracker)` | Binds a live-updating progress tracker |
| `AddButton(text, argument, style?)` | Adds an action button |
| `AddButton(Action<ToastButtonBuilder>)` | Adds a button via sub-builder (icon, tooltip, input binding) |
| `AddContextMenuItem(text, argument)` | Adds a right-click context menu item |
| `AddSnoozeButton(selectionInputId?)` | Adds a system snooze button |
| `AddDismissButton()` | Adds a system dismiss button |
| `AddTextInput(id, placeholder?, title?, defaultValue?)` | Adds a text input field |
| `AddSelectionInput(id, selections, title?, defaultValue?)` | Adds a dropdown selection input |
| `AddGroup(Action<ToastGroupBuilder>)` | Adds an adaptive group for multi-column layout |

### ToastButtonBuilder

| Method | Description |
|---|---|
| `Text(string)` | Sets the button label |
| `Argument(string)` | Sets the activation argument |
| `ActivationType(ToastActivationType)` | Sets activation type |
| `Icon(string uri)` | Sets a button icon |
| `Style(ToastButtonStyle)` | Sets visual style (`Default`, `Success`, `Critical`) |
| `Tooltip(string)` | Sets tooltip text (Windows 11+) |
| `ForInput(string inputId)` | Associates the button with an input field (quick-reply pattern) |

## API Reference: IToastHandle

| Member | Type | Description |
|---|---|---|
| `Tag` | `string` | Tag identifying this toast |
| `Group` | `string?` | Group this toast belongs to |
| `State` | `ToastHandleState` | `Active`, `Dismissed`, `Failed`, or `Removed` |
| `SuppressPopup` | `bool` | Whether popup was suppressed |
| `Priority` | `ToastPriority` | `Default` or `High` |
| `ExpiresOnReboot` | `bool` | Whether toast is removed on reboot |
| `ExpirationTime` | `DateTimeOffset?` | When toast auto-expires (null = 3-day default) |
| `Update(Dictionary<string, string>)` | method | Updates data bindings (for progress bars) |
| `Update(Action<ToastBuilder>)` | method | Replaces toast content (keeps same tag/group) |
| `Hide()` | method | Dismisses toast immediately from screen and Action Center |
| `Remove()` | method | Removes toast from Action Center by tag |
| `RemoveGroup()` | method | Removes all toasts in this toast's group |
| `Activated` | event | Raised when user clicks a button or the toast body |
| `Dismissed` | event | Raised when toast is dismissed (user, timeout, or app) |
| `Failed` | event | Raised when toast fails to display (error code as `int`) |

### IToastService

| Member | Description |
|---|---|
| `Show(ToastContent)` | Shows a toast from a pre-built content object |
| `Show(Action<ToastBuilder>)` | Shows a toast using the fluent builder |
| `Active` | `IReadOnlyList<IToastHandle>` of all active toasts |
| `ClearHistory()` | Clears all toasts from Action Center for this application |
