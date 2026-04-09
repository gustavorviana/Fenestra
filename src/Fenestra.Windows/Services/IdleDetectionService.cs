using Fenestra.Core;
using Fenestra.Windows.Native;

namespace Fenestra.Windows.Services;

/// <summary>
/// <see cref="IIdleDetectionService"/> implementation that uses <see cref="System.Threading.Timer"/>
/// (pool-based, framework-agnostic) to poll an <see cref="IIdleInputProbe"/>.
///
/// <para>
/// Not tied to WPF — lives in <c>Fenestra.Windows</c> so it can be reused by WinForms,
/// console, and background Windows hosts without duplication.
/// </para>
///
/// <para>
/// Events: if an <see cref="IThreadContext"/> is provided (typical in WPF apps where
/// <c>WpfFenestraBuilder</c> registers it), events are marshalled to the dispatcher thread
/// via <see cref="IThreadContext.InvokeAsync"/>. Otherwise (console, background),
/// events fire on the thread pool timer thread and the caller is responsible for marshalling.
/// </para>
/// </summary>
internal sealed class IdleDetectionService : FenestraComponent, IIdleDetectionService
{
    private static readonly TimeSpan MinThreshold = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MinPollInterval = TimeSpan.FromMilliseconds(100);

    private readonly IIdleInputProbe _probe;
    private readonly IThreadContext? _threadContext;
    private readonly System.Threading.Timer _timer;
    private TimeSpan _threshold;
    private TimeSpan _idleTime;
    private bool _isIdle;

    public event EventHandler? BecameIdle;
    public event EventHandler? BecameActive;

    public TimeSpan IdleTime => _idleTime;
    public bool IsIdle => _isIdle;

    public TimeSpan Threshold
    {
        get => _threshold;
        set
        {
            if (value < MinThreshold)
                throw new ArgumentException(
                    $"Threshold must be at least {MinThreshold.TotalSeconds}s.", nameof(value));
            _threshold = value;
            // Re-evaluation happens on next Poll tick, not immediately on assignment.
        }
    }

    public IdleDetectionService(
        IdleDetectionOptions options,
        IThreadContext? threadContext = null,
        IIdleInputProbe? probe = null)
    {
        Platform.EnsureWindows();
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (options.Threshold < MinThreshold)
            throw new ArgumentException(
                $"Threshold must be at least {MinThreshold.TotalSeconds}s.", nameof(options));
        if (options.PollInterval < MinPollInterval)
            throw new ArgumentException(
                $"PollInterval must be at least {MinPollInterval.TotalMilliseconds}ms.", nameof(options));

        _probe = probe ?? new IdleInputProbe();
        _threadContext = threadContext;
        _threshold = options.Threshold;

        // System.Threading.Timer fires callbacks on the thread pool — no dispatcher needed.
        // First tick after one PollInterval (same as period).
        _timer = new System.Threading.Timer(_ => SafePoll(), state: null, options.PollInterval, options.PollInterval);
    }

    private void SafePoll()
    {
        if (Disposed) return;
        try
        {
            Poll();
        }
        catch (Exception ex)
        {
            // Swallow: we must not propagate exceptions out of the timer callback — it would
            // crash the thread pool thread and leave the timer in an undefined state.
            System.Diagnostics.Debug.WriteLine($"[Fenestra.IdleDetection] Poll failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Exposed for testing — normally invoked by the internal timer every PollInterval.
    /// Reads the probe, updates <see cref="IdleTime"/>/<see cref="IsIdle"/>, and raises
    /// transition events when the idle state flips.
    /// </summary>
    internal void Poll()
    {
        if (Disposed) return;

        _idleTime = _probe.GetIdleTime();
        bool nowIdle = _idleTime >= _threshold;

        if (!_isIdle && nowIdle)
        {
            _isIdle = true;
            RaiseEvent(BecameIdle);
        }
        else if (_isIdle && !nowIdle)
        {
            _isIdle = false;
            RaiseEvent(BecameActive);
        }
        // Else: no transition, no event fired.
    }

    private void RaiseEvent(EventHandler? handler)
    {
        if (handler is null) return;

        if (_threadContext is not null)
        {
            // Marshal to the dispatcher/sync-context thread so consumers can update UI
            // without manual marshalling. Fire-and-forget: errors are swallowed by the
            // thread context to avoid crashing the timer thread.
            _ = _threadContext.InvokeAsync(() => handler(this, EventArgs.Empty));
        }
        else
        {
            // No thread context provided — raise synchronously on the timer thread.
            // Caller is responsible for marshalling to UI if needed.
            handler(this, EventArgs.Empty);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
