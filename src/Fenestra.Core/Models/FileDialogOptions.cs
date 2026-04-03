namespace Fenestra.Core.Models;

public class FileDialogOptions
{
    public string? Title { get; set; }
    public string? DefaultExtension { get; set; }
    public string? InitialDirectory { get; set; }
    public string? FileName { get; set; }
    public bool AddToRecent { get; set; } = true;
    public bool CheckPathExists { get; set; } = true;
    public FileExtensionInfo[]? Extensions { get; set; }
}

public class OpenFileDialogOptions : FileDialogOptions
{
    public bool Multiselect { get; set; }
    public bool CheckFileExists { get; set; } = true;
    public bool ReadOnlyChecked { get; set; }
    public bool ShowReadOnly { get; set; }
}

public class SaveFileDialogOptions : FileDialogOptions
{
    public bool OverwritePrompt { get; set; } = true;
    public bool CreatePrompt { get; set; }
}

public class FolderDialogOptions
{
    public string? Title { get; set; }
    public string? InitialDirectory { get; set; }
    public bool Multiselect { get; set; }
}
