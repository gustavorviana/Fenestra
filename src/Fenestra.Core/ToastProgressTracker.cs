namespace Fenestra.Core;

/// <summary>
/// Tracks progress and automatically updates the bound toast notification.
/// Bind to a toast via <see cref="ToastBuilder.BindProgress"/>.
/// </summary>
public class ToastProgressTracker
{
    private Action<Dictionary<string, string>>? _updateCallback;
    private uint _sequenceNumber;

    /// <summary>
    /// Reports progress value only (0.0 to 1.0).
    /// </summary>
    public void Report(double value)
    {
        SendUpdate(value, null, null, null);
    }

    /// <summary>
    /// Reports status text only.
    /// </summary>
    public void Report(string status)
    {
        SendUpdate(null, status, null, null);
    }

    /// <summary>
    /// Reports progress value and status text.
    /// </summary>
    public void Report(double value, string status)
    {
        SendUpdate(value, status, null, null);
    }

    /// <summary>
    /// Reports progress with all fields.
    /// </summary>
    public void Report(double value, string status, string? title = null, string? valueOverride = null)
    {
        SendUpdate(value, status, title, valueOverride);
    }

    internal void Bind(Action<Dictionary<string, string>> updateCallback)
    {
        _updateCallback = updateCallback;
    }

    private void SendUpdate(double? value, string? status, string? title, string? valueOverride)
    {
        if (_updateCallback == null) return;

        _sequenceNumber++;
        var data = new Dictionary<string, string>();

        if (value.HasValue)
            data["progressValue"] = value.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        if (status != null)
            data["progressStatus"] = status;
        if (title != null)
            data["progressTitle"] = title;
        if (valueOverride != null)
            data["progressValueOverride"] = valueOverride;

        _updateCallback(data);
    }
}
