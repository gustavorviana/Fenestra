using Fenestra.Core;
using Fenestra.Windows.Native;
using System.Diagnostics;
using System.IO;

namespace Fenestra.Windows.Services;

/// <summary>
/// Default <see cref="ITaskbarOverlayService"/> implementation backed by the
/// <c>ITaskbarList3::SetOverlayIcon</c> COM API. Framework-agnostic — resolves the
/// host HWND via <see cref="Process.MainWindowHandle"/>, so it works in WPF, WinForms,
/// Avalonia, or any other host.
/// </summary>
/// <remarks>
/// <c>ITaskbarList3</c> is STA-affine: the COM object must be created and used from the
/// same STA thread. When an <see cref="IThreadContext"/> is registered (e.g. by the WPF
/// builder), all calls are marshalled through it onto the UI thread. Without one, the
/// caller is responsible for invoking the service on an STA-compatible thread.
/// </remarks>
internal sealed class TaskbarOverlayService : ITaskbarOverlayService
{
    private readonly object _lock = new();
    private readonly IThreadContext? _threadContext;
    private ITaskbarList3? _taskbarList;
    private IntPtr _ownedIcon = IntPtr.Zero;

    public TaskbarOverlayService(IThreadContext? threadContext = null)
    {
        _threadContext = threadContext;
    }

    public void SetOverlay(string iconPath, string? accessibilityText = null)
    {
        if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
            return;

        InvokeOnUi(() =>
        {
            var hIcon = ShellNativeMethods.LoadImage(
                IntPtr.Zero,
                iconPath,
                ShellNativeMethods.IMAGE_ICON,
                16, 16,
                ShellNativeMethods.LR_LOADFROMFILE);

            if (hIcon == IntPtr.Zero)
                return;

            ApplyOverlayOnUi(hIcon, accessibilityText);
        });
    }

    public void SetOverlay(IntPtr hIcon, string? accessibilityText = null)
    {
        InvokeOnUi(() =>
        {
            if (hIcon == IntPtr.Zero)
            {
                // Passing Zero is an explicit clear request.
                var hwnd = GetMainWindowHandle();
                if (hwnd != IntPtr.Zero)
                {
                    EnsureTaskbarList();
                    _taskbarList?.SetOverlayIcon(hwnd, IntPtr.Zero, null);
                }
                ReleaseOwnedIcon();
                return;
            }

            ApplyOverlayOnUi(hIcon, accessibilityText);
        });
    }

    public void Clear()
    {
        InvokeOnUi(() =>
        {
            var hwnd = GetMainWindowHandle();
            if (hwnd == IntPtr.Zero) return;

            EnsureTaskbarList();
            _taskbarList?.SetOverlayIcon(hwnd, IntPtr.Zero, null);
            ReleaseOwnedIcon();
        });
    }

    /// <summary>
    /// Runs the given action on the UI thread (via the registered
    /// <see cref="IThreadContext"/>, if any), or synchronously on the caller when no
    /// context is available. Swallows <see cref="InvalidOperationException"/> from the
    /// thread context so headless/test scenarios are safe no-ops.
    /// </summary>
    private void InvokeOnUi(Action action)
    {
        if (_threadContext is null)
        {
            action();
            return;
        }

        try { _threadContext.Invoke(action); }
        catch (InvalidOperationException) { /* no main thread — headless/test */ }
    }

    private void ApplyOverlayOnUi(IntPtr hIcon, string? description)
    {
        var hwnd = GetMainWindowHandle();
        if (hwnd == IntPtr.Zero)
        {
            // Don't leak the HICON we just loaded.
            ShellNativeMethods.DestroyIcon(hIcon);
            throw new InvalidOperationException(
                "Taskbar overlay requires a visible main window. " +
                "Call SetOverlay after the window has been shown.");
        }

        EnsureTaskbarList();
        if (_taskbarList is null)
        {
            // COM creation failed — silently drop (e.g. restricted session, win < 7).
            ShellNativeMethods.DestroyIcon(hIcon);
            return;
        }

        _taskbarList.SetOverlayIcon(hwnd, hIcon, description);

        ReleaseOwnedIcon();
        _ownedIcon = hIcon;
    }

    private static IntPtr GetMainWindowHandle()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.MainWindowHandle;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private void EnsureTaskbarList()
    {
        lock (_lock)
        {
            _taskbarList ??= TaskbarListFactory.TryCreate();
        }
    }

    private void ReleaseOwnedIcon()
    {
        if (_ownedIcon != IntPtr.Zero)
        {
            ShellNativeMethods.DestroyIcon(_ownedIcon);
            _ownedIcon = IntPtr.Zero;
        }
    }
}
