using Fenestra.Core.Drawing;

namespace Fenestra.Core.Tray;

public interface INotifyBadge
{
    int Quantity { get; set; }
    bool IsDot { get; }

    FenestralColor Background { get; set; }
    FenestralColor Foreground { get; set; }

    void SetDot();

    void Clear();
}