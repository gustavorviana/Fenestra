namespace Fenestra.Windows;

/// <summary>
/// Tracks progress and automatically updates the bound toast notification.
/// Bind to a toast via <see cref="ToastBuilder.BindProgress"/>.
/// </summary>
public class ToastProgressTracker
{
    private volatile Action<Dictionary<string, string>>? _updateCallback;

    /// <summary>
    /// The fixed title displayed above the progress bar. Null to omit.
    /// </summary>
    public string? Title { get; }

    public double Value { get; private set; }

    /// <summary>
    /// Whether to include the value override binding (custom text instead of percentage).
    /// </summary>
    public bool UseValueOverride { get; }

    /// <summary>
    /// Creates a progress tracker with optional title and value override support.
    /// </summary>
    /// <param name="title">Fixed title displayed above the progress bar. Null to omit.</param>
    /// <param name="useValueOverride">Whether to enable custom text instead of the default percentage display.</param>
    public ToastProgressTracker(string? title = null, bool useValueOverride = false)
    {
        Title = title;
        UseValueOverride = useValueOverride;
    }

    /// <summary>
    /// Reports progress value only (0.0 to 1.0).
    /// </summary>
    public void Report(double value)
    {
        SendUpdate(value, null, null);
    }

    /// <summary>
    /// Reports status text only.
    /// </summary>
    public void Report(string status)
    {
        SendUpdate(null, status, null);
    }

    /// <summary>
    /// Reports progress value and status text.
    /// </summary>
    public void Report(double value, string status)
    {
        SendUpdate(value, status, null);
    }

    /// <summary>
    /// Reports progress value, status text, and optional value override.
    /// </summary>
    public void Report(double value, string status, string? valueOverride)
    {
        SendUpdate(value, status, valueOverride);
    }

    internal void Bind(Action<Dictionary<string, string>> updateCallback)
    {
        _updateCallback = updateCallback;
    }

    private void SendUpdate(double? value, string? status, string? valueOverride)
    {
        if (value != null)
            Value = value.Value;

        if (_updateCallback == null) return;

        var data = new Dictionary<string, string>();

        if (value.HasValue)
            data["progressValue"] = value.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        if (!string.IsNullOrEmpty(status))
            data["progressStatus"] = status!;

        if (!string.IsNullOrEmpty(Title))
            data["progressTitle"] = Title!;

        if (valueOverride != null && UseValueOverride)
            data["progressValueOverride"] = valueOverride;

        _updateCallback(data);
    }
}
