using Fenestra.Core.Drawing;

namespace Fenestra.Core.Tray
{
    public abstract class NotifyBadgeOverlayBase : FenestraComponent, INotifyBadge, ITrayIconOverlay
    {
        public event EventHandler OnUpdate = delegate { };
        private int _quantity = 0;

        private FenestralColor _background;
        private FenestralColor _foreground;

        public NotifyBadgeOverlayBase(FenestralColor? background = null, FenestralColor? foreground = null)
        {
            _background = background ?? FenestralColor.FromHex("#FF0000");
            _foreground = foreground ?? FenestralColor.FromHex("#FFFFFF");
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Quantity cannot be negative.");

                if (_quantity != value)
                {
                    _quantity = value;
                    SignalUpdated();
                }
            }
        }

        public bool IsDot => _quantity == -1;

        public FenestralColor Background
        {
            get => _background;
            set
            {
                if (_background != value)
                {
                    _background = value;
                    SignalUpdated();
                }
            }
        }

        public FenestralColor Foreground
        {
            get => _foreground; set
            {
                if (_foreground != value)
                {
                    _foreground = value;
                    SignalUpdated();
                }
            }
        }

        public void Clear()
        {
            _quantity = 0;
            SignalUpdated();
        }

        public void SetDot()
        {
            _quantity = -1;
            SignalUpdated();
        }

        protected abstract IntPtr RenderBadgedIcon(IntPtr baseHIcon);

        protected virtual void SignalUpdated()
        {
            OnUpdate(this, EventArgs.Empty);
        }

        IntPtr ITrayIconOverlay.RenderBadgedIcon(IntPtr baseHIcon)
            => RenderBadgedIcon(baseHIcon);
    }
}