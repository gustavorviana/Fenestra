using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Wpf.Native;
using Microsoft.Win32;
using System.Text;
using System.Windows;

namespace Fenestra.Wpf.Services;

internal class DialogService : IDialogService
{
    private readonly AppInfo _appInfo;

    public DialogService(AppInfo appInfo)
    {
        _appInfo = appInfo;
    }

    public FenestraMessageResult ShowMessage(
        string message,
        string? title = null,
        FenestraMessageButton buttons = FenestraMessageButton.OK,
        FenestraMessageIcon icon = FenestraMessageIcon.None,
        FenestraMessageResult defaultResult = FenestraMessageResult.OK)
    {
        var wpfResult = MessageBox.Show(
            message,
            title ?? _appInfo.AppName ?? string.Empty,
            ToWpfButton(buttons),
            ToWpfIcon(icon),
            ToWpfResult(defaultResult));

        return FromWpfResult(wpfResult);
    }

    public string? OpenFileDialog(OpenFileDialogOptions? options = null)
    {
        var dialog = new OpenFileDialog();
        ApplyOpenOptions(dialog, options);

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string[]? OpenFilesDialog(OpenFileDialogOptions? options = null)
    {
        var opts = options ?? new OpenFileDialogOptions();
        opts.Multiselect = true;

        var dialog = new OpenFileDialog();
        ApplyOpenOptions(dialog, opts);

        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }

    public string? SaveFileDialog(SaveFileDialogOptions? options = null)
    {
        var dialog = new SaveFileDialog();
        ApplySaveOptions(dialog, options);

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? OpenFolderDialog(FolderDialogOptions? options = null)
    {
        var results = ShowNativeFolderDialog(options, multiselect: false);
        return results?.Length > 0 ? results[0] : null;
    }

    public string[]? OpenFoldersDialog(FolderDialogOptions? options = null)
    {
        return ShowNativeFolderDialog(options, multiselect: true);
    }

    private static string[]? ShowNativeFolderDialog(FolderDialogOptions? options, bool multiselect)
    {
        var dialog = (FolderDialogNative.IFileOpenDialog)new FolderDialogNative.FileOpenDialogCom();

        dialog.GetOptions(out var flags);
        flags |= FolderDialogNative.FOS_PICKFOLDERS | FolderDialogNative.FOS_FORCEFILESYSTEM;
        if (multiselect || (options?.Multiselect ?? false))
            flags |= FolderDialogNative.FOS_ALLOWMULTISELECT;
        dialog.SetOptions(flags);

        if (options?.Title != null)
            dialog.SetTitle(options.Title);

        if (options?.InitialDirectory != null)
        {
            var riid = FolderDialogNative.IShellItemGuid;
            if (FolderDialogNative.SHCreateItemFromParsingName(options.InitialDirectory, IntPtr.Zero, ref riid, out var folder) == 0)
                dialog.SetFolder(folder);
        }

        var hwnd = GetActiveWindowHandle();
        if (dialog.Show(hwnd) != 0)
            return null;

        dialog.GetResults(out var shellItems);
        shellItems.GetCount(out var count);

        var paths = new string[count];
        for (uint i = 0; i < count; i++)
        {
            shellItems.GetItemAt(i, out var item);
            item.GetDisplayName(FolderDialogNative.SIGDN_FILESYSPATH, out var path);
            paths[i] = path;
        }

        return paths;
    }

    private static IntPtr GetActiveWindowHandle()
    {
        var window = Application.Current?.MainWindow;
        if (window == null) return IntPtr.Zero;
        var helper = new System.Windows.Interop.WindowInteropHelper(window);
        return helper.Handle;
    }

    private static void ApplyBaseOptions(FileDialog dialog, FileDialogOptions? options)
    {
        if (options == null) return;

        if (options.Title != null)
            dialog.Title = options.Title;

        if (options.DefaultExtension != null)
            dialog.DefaultExt = options.DefaultExtension;

        if (options.InitialDirectory != null)
            dialog.InitialDirectory = options.InitialDirectory;

        if (options.FileName != null)
            dialog.FileName = options.FileName;

        dialog.AddExtension = options.DefaultExtension != null;
        dialog.CheckPathExists = options.CheckPathExists;

#if NET8_0_OR_GREATER
        dialog.AddToRecent = options.AddToRecent;
#endif

        if (options.Extensions is { Length: > 0 })
            dialog.Filter = BuildFilterString(options.Extensions);
    }

    private static void ApplyOpenOptions(OpenFileDialog dialog, OpenFileDialogOptions? options)
    {
        ApplyBaseOptions(dialog, options);
        if (options == null) return;

        dialog.Multiselect = options.Multiselect;
        dialog.CheckFileExists = options.CheckFileExists;
        dialog.ReadOnlyChecked = options.ReadOnlyChecked;
        dialog.ShowReadOnly = options.ShowReadOnly;
    }

    private static void ApplySaveOptions(SaveFileDialog dialog, SaveFileDialogOptions? options)
    {
        ApplyBaseOptions(dialog, options);
        if (options == null) return;

        dialog.OverwritePrompt = options.OverwritePrompt;
        dialog.CreatePrompt = options.CreatePrompt;
    }

    internal static string BuildFilterString(FileExtensionInfo[] extensions)
    {
        var sb = new StringBuilder();

        foreach (var ext in extensions)
        {
            if (sb.Length > 0) sb.Append('|');
            sb.Append(ext.Description);
            sb.Append("|*.");
            sb.Append(ext.Extension);
        }

        return sb.ToString();
    }

    private static MessageBoxButton ToWpfButton(FenestraMessageButton button) => button switch
    {
        FenestraMessageButton.OK => MessageBoxButton.OK,
        FenestraMessageButton.OKCancel => MessageBoxButton.OKCancel,
        FenestraMessageButton.YesNo => MessageBoxButton.YesNo,
        FenestraMessageButton.YesNoCancel => MessageBoxButton.YesNoCancel,
        _ => MessageBoxButton.OK
    };

    private static MessageBoxImage ToWpfIcon(FenestraMessageIcon icon) => icon switch
    {
        FenestraMessageIcon.Information => MessageBoxImage.Information,
        FenestraMessageIcon.Warning => MessageBoxImage.Warning,
        FenestraMessageIcon.Error => MessageBoxImage.Error,
        FenestraMessageIcon.Question => MessageBoxImage.Question,
        _ => MessageBoxImage.None
    };

    private static MessageBoxResult ToWpfResult(FenestraMessageResult result) => result switch
    {
        FenestraMessageResult.OK => MessageBoxResult.OK,
        FenestraMessageResult.Cancel => MessageBoxResult.Cancel,
        FenestraMessageResult.Yes => MessageBoxResult.Yes,
        FenestraMessageResult.No => MessageBoxResult.No,
        _ => MessageBoxResult.None
    };

    private static FenestraMessageResult FromWpfResult(MessageBoxResult result) => result switch
    {
        MessageBoxResult.OK => FenestraMessageResult.OK,
        MessageBoxResult.Cancel => FenestraMessageResult.Cancel,
        MessageBoxResult.Yes => FenestraMessageResult.Yes,
        MessageBoxResult.No => FenestraMessageResult.No,
        _ => FenestraMessageResult.None
    };
}
