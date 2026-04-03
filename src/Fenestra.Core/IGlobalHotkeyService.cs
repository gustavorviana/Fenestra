using Fenestra.Core.Models;

namespace Fenestra.Core;

/// <summary>
/// Registers and manages system-wide keyboard hotkeys.
/// </summary>
public interface IGlobalHotkeyService : IDisposable
{
    /// <summary>
    /// Registers a global hotkey and returns its unique identifier.
    /// </summary>
    int Register(HotkeyModifiers modifiers, HotkeyKey key, Action callback);

    /// <summary>
    /// Unregisters a previously registered global hotkey by its identifier.
    /// </summary>
    void Unregister(int id);
}
