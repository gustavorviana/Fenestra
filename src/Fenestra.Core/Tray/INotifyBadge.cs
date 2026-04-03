namespace Fenestra.Core.Tray;

public interface INotifyBadge
{
    int Quantity { get; set; }
    bool IsDot { get; }

    string Background { get; set; }
    string Foreground { get; set; }

    void SetDot();

    void Clear();
}