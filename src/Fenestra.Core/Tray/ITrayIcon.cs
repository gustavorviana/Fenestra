using Fenestra.Core.Native;

namespace Fenestra.Core.Tray;

public interface ITrayIcon : IDisposable
{
    SafeIconHandle? Handle { get; }

    void Initialize();
}