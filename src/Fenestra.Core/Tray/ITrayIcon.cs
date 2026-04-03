using Fenestra.Core.Native;

namespace Fenestra.Core.Tray;

public interface ITrayIcon : IFenestraComponent
{
    SafeIconHandle? Handle { get; }

    void Initialize();
}