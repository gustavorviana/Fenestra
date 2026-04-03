namespace Fenestra.Core.Models;

/// <summary>
/// Modifier keys for global hotkey combinations.
/// </summary>
[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 1,
    Ctrl = 2,
    Shift = 4,
    Win = 8
}
