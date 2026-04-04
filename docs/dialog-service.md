# Dialog Service

## Summary

- [Overview](#overview)
- [ShowMessage](#showmessage)
- [OpenFileDialog](#openfiledialog)
- [OpenFilesDialog](#openfilesdialog)
- [SaveFileDialog](#savefiledialog)
- [OpenFolderDialog](#openfolderdialog)
- [OpenFoldersDialog](#openfoldersdialog)
- [Async Variants](#async-variants)
- [Enums Reference](#enums-reference)
  - [FenestraMessageButton](#fenestramessagebutton)
  - [FenestraMessageIcon](#fenestramessageicon)
  - [FenestraMessageResult](#fenestramessageresult)
- [FileExtensionInfo](#fileextensioninfo)
- [Dialog Options](#dialog-options)
- [Full Example](#full-example)

## Overview

`IDialogService` provides platform-abstracted message boxes and file/folder dialogs. It is registered automatically and available via constructor injection. All methods are synchronous and must run on the UI thread. Async extension methods are available for calling from background threads.

## ShowMessage

Displays a message box with configurable buttons, icon, and default result.

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IDialogService _dialogService;

    public MainWindow(IDialogService dialogService)
    {
        _dialogService = dialogService;
        Title = "Dialog Demo";
        Width = 800;
        Height = 600;
    }

    public void ShowSimpleMessage()
    {
        _dialogService.ShowMessage("Operation completed successfully.");
        // Shows an OK-only dialog with no icon
    }

    public void ShowConfirmation()
    {
        FenestraMessageResult result = _dialogService.ShowMessage(
            "Are you sure you want to delete this item?",
            title: "Confirm Delete",
            buttons: FenestraMessageButton.YesNo,
            icon: FenestraMessageIcon.Question,
            defaultResult: FenestraMessageResult.No);

        if (result == FenestraMessageResult.Yes)
        {
            // User confirmed deletion
        }
    }

    public void ShowWarning()
    {
        FenestraMessageResult result = _dialogService.ShowMessage(
            "Unsaved changes will be lost. Continue?",
            title: "Warning",
            buttons: FenestraMessageButton.YesNoCancel,
            icon: FenestraMessageIcon.Warning);

        // result => Yes, No, or Cancel
    }
}
```

## OpenFileDialog

Opens a single-file selection dialog. Returns the selected file path or `null` if cancelled.

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using System.Windows;

public class MainWindow : Window
{
    private readonly IDialogService _dialogService;

    public MainWindow(IDialogService dialogService)
    {
        _dialogService = dialogService;
        Title = "File Dialog Demo";
        Width = 800;
        Height = 600;
    }

    public void OpenFile()
    {
        string? path = _dialogService.OpenFileDialog(new OpenFileDialogOptions
        {
            Title = "Select a Document",
            InitialDirectory = @"C:\Documents",
            DefaultExtension = "txt",
            Extensions = new[]
            {
                new FileExtensionInfo("txt", "Text Files"),
                new FileExtensionInfo("md", "Markdown Files"),
                FileExtensionInfo.All
            }
        });

        if (path != null)
        {
            // path => "C:\Documents\readme.txt"
        }
    }
}
```

## OpenFilesDialog

Opens a multi-file selection dialog. Returns an array of selected paths or `null` if cancelled.

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using System.Windows;

public class MainWindow : Window
{
    private readonly IDialogService _dialogService;

    public MainWindow(IDialogService dialogService)
    {
        _dialogService = dialogService;
        Title = "Multi-File Demo";
        Width = 800;
        Height = 600;
    }

    public void ImportImages()
    {
        string[]? paths = _dialogService.OpenFilesDialog(new OpenFileDialogOptions
        {
            Title = "Select Images",
            Extensions = new[]
            {
                new FileExtensionInfo("png", "PNG Images"),
                new FileExtensionInfo("jpg", "JPEG Images"),
                FileExtensionInfo.All
            }
        });

        if (paths != null)
        {
            // paths => ["C:\Photos\photo1.png", "C:\Photos\photo2.jpg"]
        }
    }
}
```

## SaveFileDialog

Opens a save file dialog. Returns the chosen path or `null` if cancelled.

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using System.Windows;

public class MainWindow : Window
{
    private readonly IDialogService _dialogService;

    public MainWindow(IDialogService dialogService)
    {
        _dialogService = dialogService;
        Title = "Save Dialog Demo";
        Width = 800;
        Height = 600;
    }

    public void ExportReport()
    {
        string? path = _dialogService.SaveFileDialog(new SaveFileDialogOptions
        {
            Title = "Export Report",
            FileName = "report.pdf",
            DefaultExtension = "pdf",
            OverwritePrompt = true,
            Extensions = new[]
            {
                new FileExtensionInfo("pdf", "PDF Documents"),
                new FileExtensionInfo("csv", "CSV Files")
            }
        });

        if (path != null)
        {
            // path => "C:\Reports\report.pdf"
        }
    }
}
```

## OpenFolderDialog

Opens a single-folder selection dialog. Returns the selected folder path or `null` if cancelled.

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using System.Windows;

public class MainWindow : Window
{
    private readonly IDialogService _dialogService;

    public MainWindow(IDialogService dialogService)
    {
        _dialogService = dialogService;
        Title = "Folder Dialog Demo";
        Width = 800;
        Height = 600;
    }

    public void SelectOutputFolder()
    {
        string? folder = _dialogService.OpenFolderDialog(new FolderDialogOptions
        {
            Title = "Select Output Folder",
            InitialDirectory = @"C:\Projects"
        });

        if (folder != null)
        {
            // folder => "C:\Projects\output"
        }
    }
}
```

## OpenFoldersDialog

Opens a multi-folder selection dialog. Returns an array of selected paths or `null` if cancelled.

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using System.Windows;

public class MainWindow : Window
{
    private readonly IDialogService _dialogService;

    public MainWindow(IDialogService dialogService)
    {
        _dialogService = dialogService;
        Title = "Multi-Folder Demo";
        Width = 800;
        Height = 600;
    }

    public void SelectWatchFolders()
    {
        string[]? folders = _dialogService.OpenFoldersDialog(new FolderDialogOptions
        {
            Title = "Select Folders to Watch"
        });

        if (folders != null)
        {
            // folders => ["C:\Folder1", "C:\Folder2"]
        }
    }
}
```

## Async Variants

Extension methods in `Fenestra.Wpf.Extensions` marshal calls to the UI thread, making them safe to call from background threads or async contexts.

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Wpf.Extensions;
using System.Windows;

public class MainWindow : Window
{
    private readonly IDialogService _dialogService;

    public MainWindow(IDialogService dialogService)
    {
        _dialogService = dialogService;
        Title = "Async Dialog Demo";
        Width = 800;
        Height = 600;
    }

    public async Task ProcessDataAsync()
    {
        FenestraMessageResult result = await _dialogService.ShowMessageAsync(
            "Start processing?",
            title: "Confirm",
            buttons: FenestraMessageButton.YesNo,
            icon: FenestraMessageIcon.Question);

        if (result != FenestraMessageResult.Yes) return;

        string? inputFile = await _dialogService.OpenFileDialogAsync(new OpenFileDialogOptions
        {
            Title = "Select Input File",
            Extensions = new[] { new FileExtensionInfo("csv", "CSV Files") }
        });

        if (inputFile == null) return;

        string? outputFile = await _dialogService.SaveFileDialogAsync(new SaveFileDialogOptions
        {
            Title = "Save Output",
            DefaultExtension = "json",
            Extensions = new[] { new FileExtensionInfo("json", "JSON Files") }
        });

        if (outputFile == null) return;

        string? outputFolder = await _dialogService.OpenFolderDialogAsync(new FolderDialogOptions
        {
            Title = "Select Log Folder"
        });

        // All dialogs completed; proceed with processing
    }
}
```

The full list of async methods:

| Sync Method | Async Method |
|---|---|
| `ShowMessage` | `ShowMessageAsync` |
| `OpenFileDialog` | `OpenFileDialogAsync` |
| `OpenFilesDialog` | `OpenFilesDialogAsync` |
| `SaveFileDialog` | `SaveFileDialogAsync` |
| `OpenFolderDialog` | `OpenFolderDialogAsync` |
| `OpenFoldersDialog` | `OpenFoldersDialogAsync` |

## Enums Reference

### FenestraMessageButton

| Value | Description |
|---|---|
| `OK` | Single OK button |
| `OKCancel` | OK and Cancel buttons |
| `YesNo` | Yes and No buttons |
| `YesNoCancel` | Yes, No, and Cancel buttons |

### FenestraMessageIcon

| Value | Description |
|---|---|
| `None` | No icon |
| `Information` | Information icon |
| `Warning` | Warning icon |
| `Error` | Error icon |
| `Question` | Question mark icon |

### FenestraMessageResult

| Value | Description |
|---|---|
| `None` | No result (dialog was closed without selection) |
| `OK` | User clicked OK |
| `Cancel` | User clicked Cancel |
| `Yes` | User clicked Yes |
| `No` | User clicked No |

## FileExtensionInfo

A filter entry for file dialogs. Each entry has an `Extension` (e.g. `"txt"`) and a `Description` (e.g. `"Text Files"`).

Use `FileExtensionInfo.All` for a catch-all filter matching all files.

```csharp
using Fenestra.Core.Models;

var filters = new[]
{
    new FileExtensionInfo("png", "PNG Images"),
    new FileExtensionInfo("jpg", "JPEG Images"),
    new FileExtensionInfo("gif", "GIF Images"),
    FileExtensionInfo.All
};
```

## Dialog Options

### FileDialogOptions (base)

| Property | Type | Default | Description |
|---|---|---|---|
| `Title` | `string?` | `null` | Dialog title |
| `DefaultExtension` | `string?` | `null` | Extension appended when user omits one |
| `InitialDirectory` | `string?` | `null` | Starting directory |
| `FileName` | `string?` | `null` | Default file name |
| `AddToRecent` | `bool` | `true` | Add selection to recent files |
| `CheckPathExists` | `bool` | `true` | Verify path exists |
| `Extensions` | `FileExtensionInfo[]?` | `null` | Available extension filters |

### OpenFileDialogOptions

Inherits all properties from `FileDialogOptions`, plus:

| Property | Type | Default | Description |
|---|---|---|---|
| `Multiselect` | `bool` | `false` | Allow multiple file selection |
| `CheckFileExists` | `bool` | `true` | Verify selected file exists |
| `ReadOnlyChecked` | `bool` | `false` | Read-only checkbox default state |
| `ShowReadOnly` | `bool` | `false` | Show read-only checkbox |

### SaveFileDialogOptions

Inherits all properties from `FileDialogOptions`, plus:

| Property | Type | Default | Description |
|---|---|---|---|
| `OverwritePrompt` | `bool` | `true` | Prompt before overwriting |
| `CreatePrompt` | `bool` | `false` | Prompt before creating new file |

### FolderDialogOptions

| Property | Type | Default | Description |
|---|---|---|---|
| `Title` | `string?` | `null` | Dialog title |
| `InitialDirectory` | `string?` | `null` | Starting directory |
| `Multiselect` | `bool` | `false` | Allow multiple folder selection |

## Full Example

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Wpf;
using Fenestra.Wpf.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IDialogService _dialogService;

    public MainWindow(IDialogService dialogService)
    {
        _dialogService = dialogService;
        Title = "Document Editor";
        Width = 1024;
        Height = 768;
    }

    public async Task NewDocumentAsync()
    {
        FenestraMessageResult result = await _dialogService.ShowMessageAsync(
            "Discard current document and create a new one?",
            title: "New Document",
            buttons: FenestraMessageButton.YesNo,
            icon: FenestraMessageIcon.Question);

        if (result == FenestraMessageResult.Yes)
        {
            // Create new document
        }
    }

    public void OpenDocument()
    {
        string? path = _dialogService.OpenFileDialog(new OpenFileDialogOptions
        {
            Title = "Open Document",
            DefaultExtension = "txt",
            Extensions = new[]
            {
                new FileExtensionInfo("txt", "Text Documents"),
                new FileExtensionInfo("md", "Markdown Documents"),
                FileExtensionInfo.All
            }
        });

        if (path != null)
        {
            string content = File.ReadAllText(path);
            // Load content into editor
        }
    }

    public void SaveDocumentAs()
    {
        string? path = _dialogService.SaveFileDialog(new SaveFileDialogOptions
        {
            Title = "Save Document As",
            FileName = "untitled.txt",
            DefaultExtension = "txt",
            OverwritePrompt = true,
            Extensions = new[]
            {
                new FileExtensionInfo("txt", "Text Documents"),
                new FileExtensionInfo("md", "Markdown Documents")
            }
        });

        if (path != null)
        {
            File.WriteAllText(path, "document content");
        }
    }

    public void SelectWorkspace()
    {
        string? folder = _dialogService.OpenFolderDialog(new FolderDialogOptions
        {
            Title = "Select Workspace Folder"
        });

        if (folder != null)
        {
            // Set workspace root
        }
    }
}
```

## References

- [IDialogService](../src/Fenestra.Core/IDialogService.cs)
- [DialogServiceExtensions](../src/Fenestra.Windows.Wpf/Extensions/DialogServiceExtensions.cs)
- [FileDialogOptions](../src/Fenestra.Core/Models/FileDialogOptions.cs)
- [FileExtensionInfo](../src/Fenestra.Core/Models/FileExtensionInfo.cs)
- [FenestraMessageButton](../src/Fenestra.Core/Models/FenestraMessageButton.cs)
- [FenestraMessageIcon](../src/Fenestra.Core/Models/FenestraMessageIcon.cs)
- [FenestraMessageResult](../src/Fenestra.Core/Models/FenestraMessageResult.cs)
