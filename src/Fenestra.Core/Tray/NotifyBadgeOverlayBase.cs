using Fenestra.Core.Drawing;

namespace Fenestra.Core.Tray
{
    /// <summary>
    /// Base class for badge overlays that display a count or dot on the tray icon.
    /// </summary>
    public abstract class NotifyBadgeOverlayBase : FenestraComponent, INotifyBadge, ITrayIconOverlay
    {
        /// <summary>
        /// Raised when the badge state changes and the icon needs to be re-rendered.
        /// </summary>
        public event EventHandler OnUpdate = delegate { };
        private int _quantity = 0;

        private FenestralColor _background;
        private FenestralColor _foreground;

        /// <summary>
        /// Initializes the badge overlay with optional custom background and foreground colors.
        /// </summary>
        public NotifyBadgeOverlayBase(FenestralColor? background = null, FenestralColor? foreground = null)
        {
            _background = background ?? FenestralColor.FromHex("#FF0000");
            _foreground = foreground ?? FenestralColor.FromHex("#FFFFFF");
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool IsDot => _quantity == -1;

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void Clear()
        {
            _quantity = 0;
            SignalUpdated();
        }

        /// <inheritdoc />
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