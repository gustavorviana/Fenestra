using Fenestra.Core.Models;

namespace Fenestra.Core;

public interface IDialogService
{
    FenestraMessageResult ShowMessage(
        string message,
        string? title = null,
        FenestraMessageButton buttons = FenestraMessageButton.OK,
        FenestraMessageIcon icon = FenestraMessageIcon.None,
        FenestraMessageResult defaultResult = FenestraMessageResult.OK);

    string? OpenFileDialog(OpenFileDialogOptions? options = null);
    string[]? OpenFilesDialog(OpenFileDialogOptions? options = null);
    string? SaveFileDialog(SaveFileDialogOptions? options = null);
    string? OpenFolderDialog(FolderDialogOptions? options = null);
    string[]? OpenFoldersDialog(FolderDialogOptions? options = null);
}
