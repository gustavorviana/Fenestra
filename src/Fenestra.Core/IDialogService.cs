using Fenestra.Core.Models;

namespace Fenestra.Core;

/// <summary>
/// Provides methods for showing message boxes and file/folder dialogs.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a message box and returns the user's response.
    /// </summary>
    FenestraMessageResult ShowMessage(
        string message,
        string? title = null,
        FenestraMessageButton buttons = FenestraMessageButton.OK,
        FenestraMessageIcon icon = FenestraMessageIcon.None,
        FenestraMessageResult defaultResult = FenestraMessageResult.OK);

    /// <summary>
    /// Shows an open file dialog and returns the selected file path, or null if cancelled.
    /// </summary>
    string? OpenFileDialog(OpenFileDialogOptions? options = null);

    /// <summary>
    /// Shows an open file dialog allowing multiple selection and returns the selected paths, or null if cancelled.
    /// </summary>
    string[]? OpenFilesDialog(OpenFileDialogOptions? options = null);

    /// <summary>
    /// Shows a save file dialog and returns the chosen file path, or null if cancelled.
    /// </summary>
    string? SaveFileDialog(SaveFileDialogOptions? options = null);

    /// <summary>
    /// Shows a folder browser dialog and returns the selected folder path, or null if cancelled.
    /// </summary>
    string? OpenFolderDialog(FolderDialogOptions? options = null);

    /// <summary>
    /// Shows a folder browser dialog allowing multiple selection and returns the selected paths, or null if cancelled.
    /// </summary>
    string[]? OpenFoldersDialog(FolderDialogOptions? options = null);
}
