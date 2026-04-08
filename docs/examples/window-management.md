# Window Management

> Available on all platforms. `IWindowManager` and `IDialogService` are registered automatically — no builder call needed.

## Window Manager

### Show a Window

```csharp
using Fenestra.Core;

public class MainViewModel
{
    private readonly IWindowManager _windows;

    public MainViewModel(IWindowManager windows)
    {
        _windows = windows;
    }

    public void OpenSettings()
    {
        _windows.Show<SettingsWindow>();
    }
}
```

### Show a Modal Dialog

```csharp
_windows.ShowDialog<ConfirmDialog>();
```

### Get Open Windows

```csharp
var settingsWindows = _windows.GetOpenWindows<SettingsWindow>();
```

## Dialog Service

### Message Boxes

```csharp
using Fenestra.Core;

public class FileViewModel
{
    private readonly IDialogService _dialogs;

    public FileViewModel(IDialogService dialogs)
    {
        _dialogs = dialogs;
    }

    public void ConfirmDelete()
    {
        var result = _dialogs.ShowMessage(
            "Delete this file?",
            "Confirm",
            FenestraMessageButton.YesNo,
            FenestraMessageIcon.Warning);

        if (result == FenestraMessageResult.Yes)
            DeleteFile();
    }
}
```

### Open File Dialog

```csharp
// Single file
var path = _dialogs.ShowOpenFileDialog("Text Files|*.txt|All Files|*.*");
if (path != null)
    OpenFile(path);

// Multiple files
var paths = _dialogs.ShowOpenFileDialogMultiple("Images|*.png;*.jpg");
if (paths != null)
    foreach (var p in paths) LoadImage(p);
```

### Save File Dialog

```csharp
var path = _dialogs.ShowSaveFileDialog("Text Files|*.txt", defaultFileName: "document.txt");
if (path != null)
    SaveFile(path);
```

### Open Folder Dialog

```csharp
// Single folder
var folder = _dialogs.ShowOpenFolderDialog();
if (folder != null)
    LoadFolder(folder);

// Multiple folders
var folders = _dialogs.ShowOpenFolderDialogMultiple();
```
