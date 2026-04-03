using Fenestra.Core.Native;

namespace Fenestra.Core.Tray;

/// <summary>
/// Tray icon that cycles through a sequence of frames to create animation.
/// </summary>
public class AnimatedTryIcon : TrayIconBase, IAnimatedTryIcon
{
    private readonly IReadOnlyList<ITrayIcon> _icons;
    private readonly int _intervalMs;
    private Timer? _timer;
    private int _currentIndex;
    private SynchronizationContext? _syncContext;

    private event EventHandler OnIconChanged = delegate { };
    event EventHandler IAnimatedTryIcon.OnIconChanged
    {
        add => OnIconChanged += value;
        remove => OnIconChanged -= value;
    }

    protected override void UpdateHandle(SafeIconHandle handle)
    {
        base.UpdateHandle(handle);
        OnIconChanged(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public bool IsAnimating => _timer != null;

    /// <summary>
    /// Initializes a new animated tray icon with the specified frames and animation interval.
    /// </summary>
    public AnimatedTryIcon(IEnumerable<ITrayIcon> icons, int intervalMs = 500)
    {
        if (icons == null) throw new ArgumentNullException(nameof(icons));

        var list = icons.ToList();
        if (list.OfType<IAnimatedTryIcon>().Any())
            throw new InvalidOperationException("Animated icons cannot contain other animated icons.");

        _icons = list;
        _intervalMs = intervalMs;
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        foreach (var icon in _icons)
            icon.Initialize();

        if (_icons.Count > 0)
            ApplyFrame(0);
    }

    /// <inheritdoc />
    public void StartIconAnimation()
    {
        StopIconAnimation();
        if (_icons.Count == 0) return;

        _syncContext = SynchronizationContext.Current;
        _currentIndex = 0;
        ApplyFrame(_currentIndex);

        _timer = new Timer(OnTick, null, _intervalMs, _intervalMs);
    }

    /// <inheritdoc />
    public void StopIconAnimation()
    {
        var timer = _timer;
        _timer = null;
        timer?.Dispose();
    }

    private void OnTick(object? state)
    {
        if (_timer == null) return;

        if (_syncContext != null)
            _syncContext.Post(_ => AdvanceFrame(), null);
        else
            AdvanceFrame();
    }

    private void AdvanceFrame()
    {
        if (_timer == null || _icons.Count == 0) return;
        _currentIndex = (_currentIndex + 1) % _icons.Count;
        ApplyFrame(_currentIndex);
    }

    private void ApplyFrame(int index)
    {
        var icon = _icons[index];
        if (icon.Handle != null && !icon.Handle.IsInvalid)
            UpdateHandle(icon.Handle.DangerousGetHandle(), ownsHandle: false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopIconAnimation();

            foreach (var icon in _icons)
                icon.Dispose();
        }

        base.Dispose(disposing);
    }
}