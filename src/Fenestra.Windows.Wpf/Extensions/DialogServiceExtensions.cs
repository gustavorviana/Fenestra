using Fenestra.Core;
using Fenestra.Core.Models;
using System.Windows;

namespace Fenestra.Wpf.Extensions;

/// <summary>
/// Extension methods for IDialogService.
/// </summary>
public static class DialogServiceExtensions
{
    /// <summary>
    /// Shows a message box asynchronously on the UI thread.
    /// </summary>
    public static Task<FenestraMessageResult> ShowMessageAsync(
        this IDialogService dialogService,
        string message,
        string? title = null,
        FenestraMessageButton buttons = FenestraMessageButton.OK,
        FenestraMessageIcon icon = FenestraMessageIcon.None,
        FenestraMessageResult defaultResult = FenestraMessageResult.OK)
    {
        return InvokeOnUiThread(() => dialogService.ShowMessage(message, title, buttons, icon, defaultResult));
    }

    /// <summary>
    /// Opens a file dialog asynchronously on the UI thread.
    /// </summary>
    public static Task<string?> OpenFileDialogAsync(this IDialogService dialogService, OpenFileDialogOptions? options = null)
    {
        return InvokeOnUiThread(() => dialogService.OpenFileDialog(options));
    }

    /// <summary>
    /// Opens a multi-file dialog asynchronously on the UI thread.
    /// </summary>
    public static Task<string[]?> OpenFilesDialogAsync(this IDialogService dialogService, OpenFileDialogOptions? options = null)
    {
        return InvokeOnUiThread(() => dialogService.OpenFilesDialog(options));
    }

    /// <summary>
    /// Opens a save file dialog asynchronously on the UI thread.
    /// </summary>
    public static Task<string?> SaveFileDialogAsync(this IDialogService dialogService, SaveFileDialogOptions? options = null)
    {
        return InvokeOnUiThread(() => dialogService.SaveFileDialog(options));
    }

    /// <summary>
    /// Opens a folder dialog asynchronously on the UI thread.
    /// </summary>
    public static Task<string?> OpenFolderDialogAsync(this IDialogService dialogService, FolderDialogOptions? options = null)
    {
        return InvokeOnUiThread(() => dialogService.OpenFolderDialog(options));
    }

    /// <summary>
    /// Opens a multi-folder dialog asynchronously on the UI thread.
    /// </summary>
    public static Task<string[]?> OpenFoldersDialogAsync(this IDialogService dialogService, FolderDialogOptions? options = null)
    {
        return InvokeOnUiThread(() => dialogService.OpenFoldersDialog(options));
    }

    private static Task<T> InvokeOnUiThread<T>(Func<T> action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            return Task.FromResult(action());
        }

        return dispatcher.InvokeAsync(action).Task;
    }
}
