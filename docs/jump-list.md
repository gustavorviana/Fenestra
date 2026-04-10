# Jump List & Taskbar Overlay

Customizes the taskbar icon's right-click menu (Jump List) with app-defined tasks and
recent files, and displays a small overlay badge on the taskbar button (e.g. unread count).

## Table of Contents

- [Overview](#overview)
- [Requirements](#requirements)
- [Quick Start](#quick-start)
- [Jump List Tasks](#jump-list-tasks)
- [Recent Files](#recent-files)
- [Taskbar Overlay](#taskbar-overlay)
- [How It Works](#how-it-works)
- [Troubleshooting](#troubleshooting)
- [API Reference](#api-reference)

## Overview

The Jump List is the menu that appears when you right-click an application's taskbar icon.
It has two sections Fenestra exposes:

- **Tasks** — app-defined shortcuts ("New Window", "Open Settings", etc.) that relaunch
  the app with specific arguments.
- **Recent** — files recently opened by the app, populated via the shell's
  `SHAddToRecentDocs` plumbing.

The taskbar overlay is a small icon (≈16×16) drawn over the bottom-right of the taskbar
button — typically used as a badge (unread count, warning indicator, status dot).

## Requirements

Jump Lists have non-obvious plumbing requirements. **All of the following must be true**
for the Jump List to appear:

1. **Process AUMID must be set** (`SetCurrentProcessExplicitAppUserModelID`) **before
   any window is shown**. Fenestra handles this automatically — the `JumpListService`
   calls `IAumidRegistrationManager.EnsureRegistered()` (the same service the toast
   system uses), and its constructor runs during DI resolution of your main window,
   which is before the pipeline shows any visible window.

2. **A Start Menu shortcut with a matching AUMID must exist** at
   `%APPDATA%\Microsoft\Windows\Start Menu\Programs\{AppName}.lnk`. Fenestra creates or
   updates this automatically the first time `JumpListService` is constructed.

3. **The application should be pinned to the taskbar** (Windows 11 specifically).
   On Windows 11, right-clicking an *unpinned* running app shows a minimal system menu
   and may not display custom Tasks. Pin the app to the taskbar (right-click the running
   icon → "Pin to taskbar") to see the full custom Jump List. On Windows 10 the custom
   tasks show up without pinning.

4. **`Apply()` must be called** after mutating tasks or recent files. Changes are
   buffered in memory until then.

## Quick Start

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

// Program.cs (BuilderStyle)
var builder = FenestraApplication.CreateBuilder(args);
builder.UseMainWindow<MainWindow>();
builder.Services.AddWindowsJumpList();
builder.Services.AddWindowsTaskbarOverlay();
var app = builder.Build();
app.Run();
```

```csharp
// MainWindow.xaml.cs
public partial class MainWindow : Window
{
    private readonly IJumpListService _jumpList;
    private readonly ITaskbarOverlayService _overlay;

    public MainWindow(IJumpListService jumpList, ITaskbarOverlayService overlay)
    {
        InitializeComponent();
        _jumpList = jumpList;
        _overlay = overlay;

        // Configure the Jump List once, on first load.
        Loaded += (_, _) => ConfigureJumpList();
    }

    private void ConfigureJumpList()
    {
        _jumpList.SetTasks(
            new JumpListTask
            {
                Title = "New Window",
                Description = "Open a fresh instance",
                Arguments = "--new-window",
            },
            new JumpListTask
            {
                Title = "Settings",
                Description = "Open settings dialog",
                Arguments = "--settings",
            });

        _jumpList.Apply();
    }
}
```

> **Note:** `ApplicationPath` is auto-filled by `JumpListService` when left null. It
> defaults to `Process.GetCurrentProcess().MainModule.FileName` — the real host `.exe`,
> which matches the Start Menu shortcut's target. This avoids the .NET 8 quirk where
> WPF's built-in default resolves to `Assembly.GetEntryAssembly().Location` (the `.dll`),
> which Windows silently rejects. Override it only if you need a task to launch a
> different executable.

## Jump List Tasks

`JumpListTask` is a framework-agnostic POCO in `Fenestra.Windows.Models`. Key properties:

| Property            | Purpose                                                       |
|---------------------|---------------------------------------------------------------|
| `Title`             | Displayed name (required).                                    |
| `Description`       | Tooltip shown on hover.                                       |
| `ApplicationPath`   | Executable to launch. Leave `null` to auto-fill with the host `.exe`. |
| `Arguments`         | Command-line arguments passed on launch.                      |
| `WorkingDirectory`  | Working directory on launch. Defaults to the exe's directory. |
| `IconResourcePath`  | Path to `.ico`, `.exe`, or `.dll` containing the task icon.   |
| `IconResourceIndex` | Index of the icon within the resource.                        |

Combine with the `SingleInstanceGuard` service to forward the arguments to a running
instance instead of launching a new process. See `docs/single-instance.md`.

## Recent Files

```csharp
_jumpList.AddRecentFile(@"C:\Users\user\docs\report.pdf");
_jumpList.Apply();
```

Requirements for recent files to be clickable and route back to your app:

- The file must **exist and be reachable** at the time of the call. The shell silently
  drops paths it can't verify.
- The file's extension must have a **registered file-type handler** that points to your
  app — otherwise Windows shows the entries but clicking them opens the default handler,
  not your app. See `docs/file-association.md` for registration details (T2.3).
- **The AUMID must be active** when `AddRecentFile` is called. Fenestra re-ensures this
  on every call, so you don't need to worry about ordering.

To wipe the Recent list for the current AUMID:

```csharp
_jumpList.ClearRecentFiles();
_jumpList.Apply();
```

> **Warning:** `ClearRecentFiles()` wipes the shell's Recent documents list for this
> AUMID. It's a global operation, not scoped to a subset of files.

## Taskbar Overlay

```csharp
// From an .ico file on disk:
_overlay.SetOverlay(@"C:\path\to\badge.ico", "3 unread items");

// From a WPF ImageSource (constructed in code or loaded from resources):
var image = new BitmapImage(new Uri(
    "pack://application:,,,/Assets/badge-3.png", UriKind.Absolute));
_overlay.SetOverlay(image, "3 unread items");

// Clear:
_overlay.Clear();
```

The overlay requires a **visible main window** — it's keyed on the window's `HWND`, which
only exists after `Show()` is called. Call `SetOverlay` from the `Loaded` event, from
`OnReady`, or anywhere after the first frame is rendered.

Icons larger than ≈16×16 are downsampled by Windows, often with visible artifacts. Design
your badges at 16×16 natively.

## How It Works

### AUMID + shortcut registration

`JumpListService` depends on `IAumidRegistrationManager` — the same service the toast
notification system uses — for AUMID assignment and Start Menu shortcut creation. On
construction it calls `EnsureRegistered()`, which:

1. Skips entirely for packaged (MSIX) apps — the shell handles AUMID from the manifest.
2. Calls `SetCurrentProcessExplicitAppUserModelID(AppInfo.AppId)` to bind the process to
   the AUMID.
3. If the Start Menu shortcut is missing or stale, creates/updates it via
   `AppShortcutManager.CreateOrUpdateShortcut()`.

This happens during DI resolution of your main window, which in the Fenestra pipeline
runs **before** `MainWindow.Show()` — so the main window's taskbar button is created
with the correct AUMID.

### Commit flow

`Apply()` drives the Win32 shell COM surface directly — **no WPF dependency**:

1. `CoCreateInstance` → `ICustomDestinationList` (CLSID_DestinationList)
2. `BeginList` to reserve a commit session and learn the max slot count
3. `CoCreateInstance` → `IObjectCollection` (CLSID_EnumerableObjectCollection)
4. For each `JumpListTask`: `CoCreateInstance` → `IShellLinkW`, set Path/Arguments/
   Description/IconLocation, then `QueryInterface<IPropertyStore>` to set
   `System.Title` (PKEY_Title) via a `PROPVARIANT` of type `VT_LPWSTR`
5. `AddUserTasks(IObjectCollection)` followed by `AppendKnownCategory(Recent)`
6. `CommitList`

Every error path logs to `Debug.WriteLine` so silent failures can be diagnosed from the
debugger's Output → Debug window.

### Taskbar overlay

`TaskbarOverlayService` lazily creates an `ITaskbarList3` COM object, resolves the
process HWND via `Process.GetCurrentProcess().MainWindowHandle`, and calls
`ITaskbarList3::SetOverlayIcon(hwnd, hIcon, description)`. File-path inputs go through
`user32!LoadImage`; the WPF extension method in `Fenestra.Wpf.TaskbarOverlayExtensions`
renders any `ImageSource` to a 16×16 `RenderTargetBitmap` and converts to an `HICON`
via `CreateBitmap` + `CreateIconIndirect` before forwarding ownership to the service.

## Troubleshooting

### Right-clicking the taskbar icon shows nothing custom

Most common on Windows 11 for unpinned apps. **Right-click the running app's taskbar icon
→ "Pin to taskbar", close the app, relaunch it** — the Jump List should now show.

### `Debug.WriteLine` shows "AddUserTasks failed" or "CommitList failed"

Look at the HRESULT in the message. Common causes:
- `0x80070005` (E_ACCESSDENIED) — the AUMID is set but the Start Menu shortcut is
  missing or points at a different exe. Let `EnsureRegistered()` run successfully.
- `0x80040154` (REGDB_E_CLASSNOTREG) — running on an unsupported Windows version. Jump
  Lists require Windows 7 or later.

### Jump List appears but clicking tasks doesn't forward arguments to the running instance

Add `builder.Services.AddWpfSingleInstance()` and implement `ISingleInstanceApp` on your
main window to handle the forwarded arguments. See `docs/single-instance.md`.

### Taskbar overlay throws "Taskbar overlay requires a visible main window"

`SetOverlay` was called before the main window was shown. Move the call into the
`Loaded` event or into `OnReady(IServiceProvider, Window)` in a `FenestraApp` subclass.

## API Reference

### `IJumpListService` (in `Fenestra.Windows`)

| Member                                            | Description                                        |
|---------------------------------------------------|----------------------------------------------------|
| `SetTasks(params JumpListTask[] tasks)`           | Replaces the buffered tasks.                       |
| `AddRecentFile(string path)`                      | Adds a file to the shell's Recent list for this AUMID. |
| `ClearRecentFiles()`                              | Clears the Recent list for this AUMID.             |
| `Apply()`                                         | Commits buffered state to the shell.               |

### `ITaskbarOverlayService` (in `Fenestra.Windows`)

| Member                                                          | Description                                    |
|-----------------------------------------------------------------|------------------------------------------------|
| `SetOverlay(string iconPath, string? text = null)`              | Overlay from an `.ico`/`.exe`/`.dll` on disk.  |
| `SetOverlay(IntPtr hIcon, string? text = null)`                 | Overlay from an existing HICON (ownership transferred). |
| `Clear()`                                                       | Removes the current overlay.                   |

### WPF extension (in `Fenestra.Wpf`)

| Member                                                                                 | Description                             |
|----------------------------------------------------------------------------------------|-----------------------------------------|
| `SetOverlay(this ITaskbarOverlayService, ImageSource icon, string? text = null)`       | Overlay from any WPF `ImageSource` — renders to a 16×16 HICON and forwards. |

### DI Extensions

```csharp
// Framework-agnostic — in Fenestra.Windows
builder.Services.AddWindowsJumpList();        // IJumpListService + transitive IAumidRegistrationManager
builder.Services.AddWindowsTaskbarOverlay();  // ITaskbarOverlayService
```
