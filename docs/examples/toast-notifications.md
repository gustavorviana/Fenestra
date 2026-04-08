# Toast Notifications

> **Windows 10+ only.** The `Fenestra.Windows` and `Fenestra.Windows.Wpf` packages use Windows-specific APIs (WinRT COM interop) that are only available on Windows 10 version 1607 and later. Calling these APIs on Linux, macOS, or older Windows versions will throw a `PlatformNotSupportedException` at runtime.
>
> The libraries intentionally target `net6.0` / `net472` (not `net6.0-windows`) to allow future cross-platform abstraction layers. However, the current toast implementation is Windows-only.

## Table of Contents

- [Setup & Configuration](#setup--configuration)
- [Basic Toast](#basic-toast)
- [Toast with Events](#toast-with-events)
- [Buttons & Actions](#buttons--actions)
- [User Input](#user-input)
- [Images](#images)
- [Audio](#audio)
- [Progress Bars](#progress-bars)
- [Toast Management](#toast-management)
- [Scheduling](#scheduling)
- [Advanced Properties](#advanced-properties)
- [Notification Settings](#notification-settings)
- [Update Result](#update-result)
- [Headers](#headers)
- [Adaptive Layout](#adaptive-layout)

---

## Setup & Configuration

Register the toast service in your WPF application startup:

```csharp
using Fenestra.Windows.Wpf;

var builder = FenestraBuilder.CreateDefault()
    .UseAppInfo("My App", new Version(1, 0, 0))
    .UseToastNotifications();       // Registers IToastService
    // .UseToastActivation();       // Optional: enables background activation when app is closed

var app = builder.Build();
app.Run();
```

Then inject `IToastService` wherever you need it:

```csharp
using Fenestra.Windows;

public class MainViewModel
{
    private readonly IToastService _toast;

    public MainViewModel(IToastService toast)
    {
        _toast = toast;
    }
}
```

---

## Basic Toast

The simplest toast — just a title and body:

```csharp
_toast.Show(t => t
    .Title("Hello!")
    .Body("This is a toast notification."));
```

If you don't need to interact with the toast after showing it, you can ignore the returned handle. A tag is auto-generated.

To set a specific tag for later reference:

```csharp
_toast.Show(t => t
    .Title("Download Complete")
    .Body("report.pdf has been saved.")
    .Tag("download-report"));
```

---

## Toast with Events

Subscribe to `Activated`, `Dismissed`, and `Failed` events to react to user interaction:

```csharp
var handle = _toast.Show(t => t
    .Title("New Message")
    .Body("You have 3 unread messages.")
    .Launch("action=open-inbox"));

handle.Activated += (sender, args) =>
{
    // args.Arguments contains the launch string ("action=open-inbox")
    Console.WriteLine($"Clicked! Arguments: {args.Arguments}");
};

handle.Dismissed += (sender, reason) =>
{
    // reason: UserCanceled, ApplicationHidden, or TimedOut
    Console.WriteLine($"Dismissed: {reason}");
};

handle.Failed += (sender, errorCode) =>
{
    Console.WriteLine($"Failed with HRESULT: 0x{errorCode:X8}");
};
```

> Events are marshaled to the UI thread automatically. You can safely update UI controls inside event handlers.

---

## Buttons & Actions

### Simple Action Buttons

```csharp
_toast.Show(t => t
    .Title("Incoming File")
    .Body("Would you like to save report.pdf?")
    .AddButton("Save", "action=save")
    .AddButton("Ignore", "action=ignore"));
```

### Styled Buttons (Success / Critical)

```csharp
_toast.Show(t => t
    .Title("Deploy to Production?")
    .Body("This will affect all users.")
    .AddButton("Deploy", "action=deploy", ToastButtonStyle.Critical)
    .AddButton("Cancel", "action=cancel", ToastButtonStyle.Success));
```

### Advanced Button Configuration

```csharp
_toast.Show(t => t
    .Title("Quick Reply")
    .Body("John: Hey, are you free?")
    .AddTextInput("reply", placeholder: "Type a reply...")
    .AddButton(b => b
        .Text("Send")
        .Argument("action=reply")
        .ForInput("reply")            // Associates button with input field
        .Icon("ms-appx:///send.png")
        .ActivationType(ToastActivationType.Background)));
```

### Context Menu Items

Right-click menu items:

```csharp
_toast.Show(t => t
    .Title("Reminder")
    .Body("Team meeting in 15 minutes")
    .AddContextMenuItem("Snooze 5 min", "action=snooze&minutes=5")
    .AddContextMenuItem("Dismiss", "action=dismiss"));
```

### System Snooze & Dismiss Buttons

```csharp
_toast.Show(t => t
    .Title("Alarm")
    .Body("Wake up!")
    .Scenario(ToastScenario.Alarm)
    .AddSnoozeButton()
    .AddDismissButton());
```

---

## User Input

### Text Input

```csharp
var handle = _toast.Show(t => t
    .Title("Feedback")
    .Body("How was your experience?")
    .AddTextInput("feedback", placeholder: "Type here...", title: "Your feedback")
    .AddButton("Submit", "action=submit"));

handle.Activated += (_, args) =>
{
    if (args.UserInput.TryGetValue("feedback", out var feedback))
        Console.WriteLine($"User typed: {feedback}");
};
```

### Selection Input (Dropdown)

```csharp
var handle = _toast.Show(t => t
    .Title("Set Status")
    .AddSelectionInput("status", new Dictionary<string, string>
    {
        ["available"] = "Available",
        ["busy"] = "Busy",
        ["dnd"] = "Do Not Disturb",
        ["away"] = "Away"
    }, title: "Your status", defaultValue: "available")
    .AddButton("Set", "action=set-status"));

handle.Activated += (_, args) =>
{
    if (args.UserInput.TryGetValue("status", out var status))
        Console.WriteLine($"Selected: {status}");   // e.g. "busy"
};
```

---

## Images

### Inline Image (Full Width)

```csharp
_toast.Show(t => t
    .Title("Photo Shared")
    .Body("Alice shared a photo with you.")
    .InlineImage("C:\\Users\\me\\Photos\\sunset.jpg", alt: "Sunset photo"));
```

### Hero Image (Wide Banner)

```csharp
_toast.Show(t => t
    .Title("Breaking News")
    .Body("Major event happening now.")
    .HeroImage("https://example.com/banner.jpg"));
```

### App Logo (Square or Circle)

```csharp
_toast.Show(t => t
    .Title("Alice")
    .Body("Hey! Are you coming tonight?")
    .AppLogo("C:\\Users\\me\\Photos\\alice.jpg", ToastImageCrop.Circle));
```

---

## Audio

### System Sounds

```csharp
// Available sounds: Default, IM, Mail, Reminder, SMS,
//                   Alarm1-10, Call1-10

_toast.Show(t => t
    .Title("Incoming Call")
    .Body("John is calling...")
    .Audio(ToastAudio.Call2)
    .Duration(ToastDuration.Long));
```

### Custom Audio

```csharp
_toast.Show(t => t
    .Title("Custom Sound")
    .AudioCustom("ms-appx:///Assets/notification.wav"));
```

### Silent Toast

```csharp
_toast.Show(t => t
    .Title("Silent Update")
    .Body("This won't make a sound.")
    .Silent());
```

### Looping Audio (Requires Long Duration)

```csharp
_toast.Show(t => t
    .Title("Alarm!")
    .Body("Time to wake up.")
    .Scenario(ToastScenario.Alarm)
    .Audio(ToastAudio.Alarm5)
    .AudioLoop()
    .Duration(ToastDuration.Long));
```

---

## Progress Bars

### Static Progress Bar

```csharp
_toast.Show(t => t
    .Title("Uploading...")
    .Progress("Preparing files", value: 0.0, title: "report.pdf"));
```

### Dynamic Progress (Auto-Updating)

Use `ToastProgressTracker` to update the progress bar after the toast is shown:

```csharp
var tracker = new ToastProgressTracker(title: "report.pdf", useValueOverride: true);

var handle = _toast.Show(t => t
    .Title("Uploading...")
    .BindProgress(tracker));

// Update progress from anywhere (e.g., inside a download loop)
tracker.Report(0.25, "Uploading...", "25%");
tracker.Report(0.50, "Uploading...", "50%");
tracker.Report(0.75, "Uploading...", "75%");
tracker.Report(1.00, "Complete!", "100%");
```

`Report` overloads:
- `Report(double value)` — value only (0.0 to 1.0)
- `Report(string status)` — status text only
- `Report(double value, string status)` — value + status
- `Report(double value, string status, string? valueOverride)` — value + status + custom text display

---

## Toast Management

### Find Active Toasts

```csharp
// Find by tag
var handle = _toast.FindByTag("download-report");
if (handle != null)
    Console.WriteLine($"State: {handle.State}");   // Active, Dismissed, Failed, or Removed

// Find by group
var groupHandles = _toast.FindByGroup("downloads");

// List all active
foreach (var active in _toast.Active)
    Console.WriteLine($"[{active.Tag}] {active.State}");
```

### Hide (Dismiss Immediately)

Removes the toast from the screen **and** Action Center:

```csharp
handle.Hide();
```

### Remove (From Action Center)

Removes from Action Center. The toast may still be visible on screen until it times out:

```csharp
handle.Remove();
```

### Remove Group

Removes all toasts in the same group from Action Center:

```csharp
handle.RemoveGroup();
```

### Clear History

```csharp
// Clear all toasts for this app
_toast.ClearHistory();

// Clear all toasts in a specific group
_toast.ClearHistory("downloads");

// Clear a specific toast by tag and group
_toast.ClearHistory("file-1", "downloads");
```

### Dispose (Alias for Remove)

`IToastHandle` implements `IDisposable`. Disposing removes it from Action Center:

```csharp
using var handle = _toast.Show(t => t.Title("Temporary").Body("Gone on dispose."));
// handle.Remove() is called automatically at end of scope
```

---

## Scheduling

Schedule a toast to appear at a future time:

```csharp
var scheduled = _toast.Schedule(
    t => t.Title("Reminder").Body("Meeting starts in 5 minutes!").Tag("meeting-reminder"),
    deliveryTime: DateTimeOffset.Now.AddMinutes(5));

// Check scheduled properties
Console.WriteLine($"Scheduled for: {scheduled.DeliveryTime}");
Console.WriteLine($"Tag: {scheduled.Tag}");

// Cancel if no longer needed
scheduled.Cancel();
```

List all scheduled toasts:

```csharp
foreach (var s in _toast.Scheduled)
    Console.WriteLine($"[{s.Tag}] at {s.DeliveryTime}");
```

---

## Advanced Properties

### Priority

```csharp
// ToastPriority: Default, High
_toast.Show(t => t
    .Title("Urgent Alert")
    .Body("Server is down!")
    .Priority(ToastPriority.High));
```

### Expiration

```csharp
_toast.Show(t => t
    .Title("Limited Offer")
    .Body("Expires in 1 hour.")
    .ExpirationTime(DateTimeOffset.Now.AddHours(1)));

_toast.Show(t => t
    .Title("Session Info")
    .Body("This toast won't survive a reboot.")
    .ExpiresOnReboot());
```

### Suppress Popup (Silent Delivery to Action Center)

```csharp
_toast.Show(t => t
    .Title("Background Update")
    .Body("New data available.")
    .SuppressPopup());
```

### Notification Mirroring (Cross-Device)

```csharp
// NotificationMirroring: Allowed (default), Disabled

_toast.Show(t => t
    .Title("Local Only")
    .Body("This won't appear on your other devices.")
    .NotificationMirroring(NotificationMirroring.Disabled));
```

### Remote ID (Cross-Device Matching)

```csharp
_toast.Show(t => t
    .Title("Synced Notification")
    .Body("Matched across devices by remote ID.")
    .RemoteId("chat-message-42"));
```

### Scenario

```csharp
// ToastScenario: Default, Reminder, Alarm, IncomingCall, Urgent

_toast.Show(t => t
    .Title("Team Meeting")
    .Body("Starting in 5 minutes")
    .Scenario(ToastScenario.Reminder)
    .AddSnoozeButton()
    .AddDismissButton());
```

### Duration

```csharp
// ToastDuration: Short (default ~7s), Long (~25s)

_toast.Show(t => t
    .Title("Important Notice")
    .Body("Please read carefully.")
    .Duration(ToastDuration.Long));
```

### Custom Timestamp

Override the time displayed in Action Center:

```csharp
_toast.Show(t => t
    .Title("Email from Alice")
    .Body("Hey, check this out!")
    .Timestamp(DateTimeOffset.Now.AddMinutes(-30)));  // Shows "30 minutes ago"
```

### Groups

Organize toasts into groups for bulk operations:

```csharp
_toast.Show(t => t
    .Title("File 1 downloaded")
    .Tag("file-1")
    .Group("downloads"));

_toast.Show(t => t
    .Title("File 2 downloaded")
    .Tag("file-2")
    .Group("downloads"));

// Remove all download notifications at once
_toast.ClearHistory("downloads");
```

---

## Notification Settings

Check if notifications are enabled before showing:

```csharp
// NotificationSetting: Enabled, DisabledForApplication, DisabledForUser,
//                      DisabledByGroupPolicy, DisabledByManifest

var setting = _toast.GetSetting();

if (setting != NotificationSetting.Enabled)
{
    Console.WriteLine($"Notifications are disabled: {setting}");
    // Show an in-app message instead
    return;
}

_toast.Show(t => t.Title("Ready").Body("Notifications are working!"));
```

---

## Update Result

Check if a toast update succeeded:

```csharp
// NotificationUpdateResult: Succeeded, Failed, NotificationNotFound

var handle = _toast.Show(t => t
    .Title("Downloading...")
    .Tag("download-1")
    .BindProgress(new ToastProgressTracker("file.zip")));

// Later, update the progress data
var result = handle.Update(new Dictionary<string, string>
{
    ["progressValue"] = "0.5",
    ["progressStatus"] = "Downloading...",
    ["progressValueOverride"] = "50%"
});

if (result != NotificationUpdateResult.Succeeded)
    Console.WriteLine($"Update failed: {result}");
```

You can also replace the entire toast content (keeps the same tag/group):

```csharp
var result = handle.Update(t => t
    .Title("Download Complete!")
    .Body("file.zip is ready."));
```

---

## Headers

Group toasts under a header in Action Center:

```csharp
_toast.Show(t => t
    .Header("emails", "Inbox", "action=open-inbox")
    .Title("Alice")
    .Body("Meeting tomorrow?"));

_toast.Show(t => t
    .Header("emails", "Inbox", "action=open-inbox")
    .Title("Bob")
    .Body("Code review ready"));

// Both toasts appear under the "Inbox" header in Action Center
```

---

## Adaptive Layout

Create multi-column layouts with groups and subgroups:

```csharp
_toast.Show(t => t
    .Title("Weather Forecast")
    .AddGroup(g => g
        .AddSubgroup(s => s
            .Weight(1)
            .TextStacking(ToastTextStacking.Center)
            .AddText("Mon", ToastTextStyle.Caption)
            .AddText("21C", ToastTextStyle.Body)
            .AddImage("ms-appx:///Assets/sunny.png"))
        .AddSubgroup(s => s
            .Weight(1)
            .TextStacking(ToastTextStacking.Center)
            .AddText("Tue", ToastTextStyle.Caption)
            .AddText("18C", ToastTextStyle.Body)
            .AddImage("ms-appx:///Assets/cloudy.png"))
        .AddSubgroup(s => s
            .Weight(1)
            .TextStacking(ToastTextStacking.Center)
            .AddText("Wed", ToastTextStyle.Caption)
            .AddText("15C", ToastTextStyle.Body)
            .AddImage("ms-appx:///Assets/rainy.png"))));
```

Available text styles: `Default`, `Caption`, `CaptionSubtle`, `Body`, `BodySubtle`, `Base`, `BaseSubtle`, `Subtitle`, `SubtitleSubtle`, `Title`, `TitleSubtle`, `Subheader`, `SubheaderSubtle`, `Header`, `HeaderSubtle`

Available text alignment: `Default`, `Auto`, `Left`, `Center`, `Right`

Available text stacking: `Default`, `Top`, `Center`, `Bottom`
