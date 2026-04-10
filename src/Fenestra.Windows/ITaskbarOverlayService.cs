namespace Fenestra.Windows;

/// <summary>
/// Displays a small overlay icon on the application's taskbar button — typically used
/// as a badge (e.g. unread notification count, status indicator). Uses the Windows
/// <c>ITaskbarList3::SetOverlayIcon</c> COM API under the hood.
/// </summary>
/// <remarks>
/// <para>
/// Framework-agnostic: lives in <c>Fenestra.Windows</c> and takes raw file paths or
/// HICON handles. See <c>Fenestra.Wpf.TaskbarOverlayExtensions</c> for WPF-specific
/// overloads that accept <c>ImageSource</c> inputs.
/// </para>
/// <para>
/// The overlay is displayed at the bottom-right of the taskbar icon and is sized around
/// 16×16 pixels — use small, high-contrast icons for readability. Icons larger than the
/// slot are downsampled by Windows, often with poor results.
/// </para>
/// <para>
/// Requires a visible main window: the overlay is keyed on the process's main window
/// HWND (<see cref="System.Diagnostics.Process.MainWindowHandle"/>), which only exists
/// after the window has been shown. Call the setters from your framework's "window
/// loaded" hook.
/// </para>
/// </remarks>
public interface ITaskbarOverlayService
{
    /// <summary>
    /// Loads an icon from a file (<c>.ico</c>, <c>.exe</c>, or <c>.dll</c>) and sets it
    /// as the taskbar overlay. The icon is loaded at 16×16.
    /// </summary>
    /// <param name="iconPath">Absolute path to the icon resource.</param>
    /// <param name="accessibilityText">
    /// Optional description announced by screen readers. Keep it short ("3 unread
    /// messages", "Error", etc.).
    /// </param>
    void SetOverlay(string iconPath, string? accessibilityText = null);

    /// <summary>
    /// Sets the taskbar overlay from an already-created HICON handle. Ownership of the
    /// handle is transferred to the service — do not destroy it after calling. The
    /// previously-owned overlay icon (if any) is released.
    /// </summary>
    /// <param name="hIcon">Raw HICON. Pass <see cref="IntPtr.Zero"/> to clear.</param>
    /// <param name="accessibilityText">Optional screen-reader description.</param>
    void SetOverlay(IntPtr hIcon, string? accessibilityText = null);

    /// <summary>
    /// Removes any existing taskbar overlay and reverts the icon to its default appearance.
    /// </summary>
    void Clear();
}
