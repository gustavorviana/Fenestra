namespace Fenestra.Core.Tray;

public interface ITrayIconOverlay : IDisposable
{
    event EventHandler OnUpdate;
    IntPtr RenderBadgedIcon(IntPtr baseHIcon);
}
