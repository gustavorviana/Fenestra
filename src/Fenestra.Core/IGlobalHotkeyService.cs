using Fenestra.Core.Models;

namespace Fenestra.Core;

public interface IGlobalHotkeyService : IDisposable
{
    int Register(HotkeyModifiers modifiers, HotkeyKey key, Action callback);
    void Unregister(int id);
}
