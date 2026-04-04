using Fenestra.Core;
using Fenestra.Windows.Models;
using Fenestra.Windows.Native;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Tray;

/// <summary>
/// Base class for <see cref="ITrayIconService"/> implementations.
/// Contains all Windows shell interop (Shell_NotifyIcon, icon management,
/// balloon tips, message processing). Subclasses provide the window handle and implement
/// WPF-specific features (context menu rendering, badge rendering).
/// </summary>
public abstract class TrayIconServiceBase : FenestraComponent, ITrayIconService
{
    #region Fields
    // Callback message (WM_USER + 0x400).
    private const uint WM_TRAYMOUSEMESSAGE = 0x0800;

    // Mouse messages received in lParam (legacy mode, no NIM_SETVERSION).
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;

    // Balloon notification messages.
    private const int NIN_BALLOONUSERCLICK = 0x0405;

    private static readonly uint WM_TASKBARCREATED = ShellNativeMethods.RegisterWindowMessage("TaskbarCreated");

    private IntPtr _hwnd;
    private bool _windowInitialized;
    private ShellNativeMethods.NOTIFYICONDATA _nid;
    private SafeIconHandle? _iconHandle;
    private ITrayIconOverlay? _overlay;
    private ITrayIcon? _icon;

    // State
    private bool _visible;
    private string _tooltip = string.Empty;
    private List<TrayMenuItem>? _menuItems;

    // Click debounce
    private Timer? _clickTimer;
    private bool _doubleClickFired;
    #endregion

    #region Properties
    /// <inheritdoc />
    public TrayMenuStyle? MenuStyle { get; set; }

    protected IntPtr WindowHandle => _hwnd;
    protected IReadOnlyList<TrayMenuItem>? MenuItems => _menuItems;

    protected abstract TrayMenuStyle CreateDefaultMenuStyle();
    #endregion

    /// <inheritdoc />
    public event EventHandler? Click;
    /// <inheritdoc />
    public event EventHandler? DoubleClick;
    /// <inheritdoc />
    public event EventHandler? BalloonTipClicked;

    protected void RaiseClick() => Click?.Invoke(this, EventArgs.Empty);
    protected void RaiseDoubleClick() => DoubleClick?.Invoke(this, EventArgs.Empty);
    protected void RaiseBalloonTipClicked() => BalloonTipClicked?.Invoke(this, EventArgs.Empty);

    /// <inheritdoc />
    public void Show()
    {
        UpdateIcon(true);
    }

    /// <inheritdoc />
    public void Hide()
    {
        UpdateIcon(false);
    }

    /// <inheritdoc />
    public void SetOverlay(ITrayIconOverlay overlay)
    {
        if (_overlay != null)
            _overlay.OnUpdate -= Overlay_OnUpdate!;

        _overlay = overlay;
        overlay.OnUpdate += Overlay_OnUpdate!;
    }

    private void Overlay_OnUpdate(object? sender, EventArgs e)
    {
        if (_icon?.Handle == null || _icon.Handle.IsInvalid) return;
        ApplyIconToTray(_icon.Handle.DangerousGetHandle());
    }

    /// <inheritdoc />
    public void SetIcon(ITrayIcon icon)
    {
        DisposeIcon();

        _icon = icon;

        if (icon.Handle != null && !icon.Handle.IsInvalid)
            _nid.hIcon = icon.Handle.DangerousGetHandle();

        if (icon is IAnimatedTryIcon animated)
            animated.OnIconChanged += Icon_OnIconChanged;
    }

    private void Icon_OnIconChanged(object? sender, EventArgs e)
    {
        if (_icon?.Handle == null || _icon.Handle.IsInvalid)
            return;

        ApplyIconToTray(_icon.Handle.DangerousGetHandle());
    }

    /// <inheritdoc />
    public void SetTooltip(string text)
    {
        _tooltip = text.Length > 127 ? text.Substring(0, 127) : text;
        _nid.szTip = _tooltip;

        if (_visible)
            UpdateIcon(showIconInTray: true);
    }

    /// <inheritdoc />
    public void ShowBalloonTip(string title, string text, TrayBalloonIcon icon = TrayBalloonIcon.None, int timeoutMs = 5000)
    {
        if (!_visible) Show();

        var data = new ShellNativeMethods.NOTIFYICONDATA();
        data.cbSize = (uint)Marshal.SizeOf<ShellNativeMethods.NOTIFYICONDATA>();

        EnsureWindow();
        data.hWnd = _hwnd;
        data.uID = _nid.uID;
        data.uFlags = ShellNativeMethods.NIF_INFO;
        data.uTimeoutOrVersion = (uint)timeoutMs;
        data.szInfoTitle = title.Length > 63 ? title.Substring(0, 63) : title;
        data.szInfo = text.Length > 255 ? text.Substring(0, 255) : text;
        data.szTip = string.Empty;
        data.dwInfoFlags = icon switch
        {
            TrayBalloonIcon.Info => ShellNativeMethods.NIIF_INFO,
            TrayBalloonIcon.Warning => ShellNativeMethods.NIIF_WARNING,
            TrayBalloonIcon.Error => ShellNativeMethods.NIIF_ERROR,
            _ => ShellNativeMethods.NIIF_NONE
        };

        ShellNativeMethods.Shell_NotifyIcon(ShellNativeMethods.NIM_MODIFY, ref data);
    }

    /// <inheritdoc />
    public void SetContextMenu(IEnumerable<TrayMenuItem> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        _menuItems = items.ToList();
    }

    /// <summary>
    /// Creates the message window and returns its handle. Called once on first use.
    /// The subclass owns the window lifecycle and must dispose it in <see cref="Dispose(bool)"/>.
    /// </summary>
    protected abstract IntPtr CreateWindowHandle();

    /// <summary>
    /// Shows a context menu for the tray icon. Called on right-click.
    /// </summary>
    protected abstract void OnShowContextMenu();

    /// <summary>
    /// Processes a window message. The subclass should call this from its WndProc.
    /// Returns <c>true</c> if the message was handled.
    /// </summary>
    protected virtual bool ProcessMessage(uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYMOUSEMESSAGE)
        {
            switch ((int)lParam)
            {
                case WM_LBUTTONDBLCLK:
                    _clickTimer?.Dispose();
                    _clickTimer = null;
                    _doubleClickFired = true;
                    RaiseDoubleClick();
                    break;

                case WM_LBUTTONUP:
                    if (_doubleClickFired)
                    {
                        _doubleClickFired = false;
                        break;
                    }
                    _clickTimer?.Dispose();
                    var delay = (int)ShellNativeMethods.GetDoubleClickTime();
                    _clickTimer = new Timer(_ =>
                    {
                        _clickTimer?.Dispose();
                        _clickTimer = null;
                        RaiseClick();
                    }, null, delay, Timeout.Infinite);
                    break;

                case WM_RBUTTONUP:
                    if (_menuItems != null && _menuItems.Count > 0)
                        OnShowContextMenu();
                    break;

                case NIN_BALLOONUSERCLICK:
                    RaiseBalloonTipClicked();
                    break;
            }

            return true;
        }

        // TaskbarCreated is broadcast when Explorer restarts — re-add the icon.
        if (msg == WM_TASKBARCREATED)
        {
            UpdateIcon(_visible);
            return true;
        }

        return false;
    }

    private void UpdateIcon(bool showIconInTray)
    {
        _visible = showIconInTray;
        EnsureWindow();

        bool hasValidIcon = _nid.hIcon != IntPtr.Zero;

        var data = new ShellNativeMethods.NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<ShellNativeMethods.NOTIFYICONDATA>(),
            uCallbackMessage = WM_TRAYMOUSEMESSAGE,
            uFlags = ShellNativeMethods.NIF_MESSAGE,
            hWnd = _hwnd,
            uID = _nid.uID,
            hIcon = IntPtr.Zero,
            szTip = string.Empty,
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };

        if (hasValidIcon)
        {
            data.uFlags |= ShellNativeMethods.NIF_ICON;
            data.hIcon = _nid.hIcon;
        }

        data.uFlags |= ShellNativeMethods.NIF_TIP;
        data.szTip = _tooltip;

        if (showIconInTray && hasValidIcon)
        {
            // Try MODIFY first; if the shell lost the icon (e.g. Explorer restart), fall back to ADD.
            if (!ShellNativeMethods.Shell_NotifyIcon(ShellNativeMethods.NIM_MODIFY, ref data))
                ShellNativeMethods.Shell_NotifyIcon(ShellNativeMethods.NIM_ADD, ref data);
        }
        else
        {
            ShellNativeMethods.Shell_NotifyIcon(ShellNativeMethods.NIM_DELETE, ref data);
        }
    }

    private void EnsureIcon()
    {
        if (_iconHandle != null && !_iconHandle.IsInvalid) return;

        IntPtr hIcon = IntPtr.Zero;
        bool ownedByUs = false;

        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? System.Reflection.Assembly.GetEntryAssembly()?.Location;

        if (!string.IsNullOrWhiteSpace(exePath))
        {
            var small = new IntPtr[1];
            if (ShellNativeMethods.ExtractIconEx(exePath!, 0, null, small, 1) > 0 && small[0] != IntPtr.Zero)
            {
                hIcon = small[0];
                ownedByUs = true;
            }
        }

        if (hIcon == IntPtr.Zero)
        {
            hIcon = ShellNativeMethods.LoadIcon(IntPtr.Zero, ShellNativeMethods.IDI_APPLICATION);
            ownedByUs = false;
        }

        _iconHandle = new SafeIconHandle(hIcon, ownsHandle: ownedByUs);
        _nid.hIcon = hIcon;
    }

    private void DisposeIcon()
    {
        if (_icon is IAnimatedTryIcon animated)
            animated.OnIconChanged -= Icon_OnIconChanged;

        _icon = null;

        _iconHandle?.Dispose();
        _iconHandle = null;
        _nid.hIcon = IntPtr.Zero;
    }

    private void EnsureWindow()
    {
        if (_windowInitialized) return;

        _hwnd = CreateWindowHandle();
        _windowInitialized = true;

        _nid = new ShellNativeMethods.NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<ShellNativeMethods.NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1,
            uCallbackMessage = WM_TRAYMOUSEMESSAGE,
            szTip = string.Empty,
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };

        EnsureIcon();
    }

    /// <summary>
    /// Takes a base HICON, composites the overlay on top if set, and updates the tray.
    /// </summary>
    private void ApplyIconToTray(IntPtr baseHIcon)
    {
        if (baseHIcon == IntPtr.Zero) return;

        if (_overlay != null)
        {
            var composited = _overlay.RenderBadgedIcon(baseHIcon);
            _nid.hIcon = composited != IntPtr.Zero ? composited : baseHIcon;
        }
        else
        {
            _nid.hIcon = baseHIcon;
        }

        if (_visible) UpdateIcon(showIconInTray: true);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _clickTimer?.Dispose();
        _clickTimer = null;
        _visible = false;
        _overlay?.Dispose();
        UpdateIcon(showIconInTray: false);
        DisposeIcon();

        _hwnd = IntPtr.Zero;
        _windowInitialized = false;
        _menuItems = null;
    }
}