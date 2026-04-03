namespace Fenestra.Core.Models;

/// <summary>
/// Base options for file dialogs.
/// </summary>
public class FileDialogOptions
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the default file extension appended when the user omits one.
    /// </summary>
    public string? DefaultExtension { get; set; }

    /// <summary>
    /// Gets or sets the initial directory displayed when the dialog opens.
    /// </summary>
    public string? InitialDirectory { get; set; }

    /// <summary>
    /// Gets or sets the default file name shown in the dialog.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets whether the selected file is added to the recent files list.
    /// </summary>
    public bool AddToRecent { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the dialog verifies that the specified path exists.
    /// </summary>
    public bool CheckPathExists { get; set; } = true;

    /// <summary>
    /// Gets or sets the file extension filters available in the dialog.
    /// </summary>
    public FileExtensionInfo[]? Extensions { get; set; }
}

/// <summary>
/// Options for the open file dialog.
/// </summary>
public class OpenFileDialogOptions : FileDialogOptions
{
    /// <summary>
    /// Gets or sets whether the user can select multiple files.
    /// </summary>
    public bool Multiselect { get; set; }

    /// <summary>
    /// Gets or sets whether the dialog verifies that the selected file exists.
    /// </summary>
    public bool CheckFileExists { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the read-only checkbox is checked by default.
    /// </summary>
    public bool ReadOnlyChecked { get; set; }

    /// <summary>
    /// Gets or sets whether the dialog shows a read-only checkbox.
    /// </summary>
    public bool ShowReadOnly { get; set; }
}

/// <summary>
/// Options for the save file dialog.
/// </summary>
public class SaveFileDialogOptions : FileDialogOptions
{
    /// <summary>
    /// Gets or sets whether the dialog prompts before overwriting an existing file.
    /// </summary>
    public bool OverwritePrompt { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the dialog prompts before creating a new file.
    /// </summary>
    public bool CreatePrompt { get; set; }
}

/// <summary>
/// Options for the folder browser dialog.
/// </summary>
public class FolderDialogOptions
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the initial directory displayed when the dialog opens.
    /// </summary>
    public string? InitialDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether the user can select multiple folders.
    /// </summary>
    public bool Multiselect { get; set; }
}
