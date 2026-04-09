using Fenestra.Windows.Models;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Abstraction over the WinRT toast notifier façade, enabling mocking in tests.
/// Implemented by <see cref="NativeToastNotifier"/>.
/// </summary>
internal interface INativeToastNotifier : IDisposable
{
    /// <summary>Direct access to the toast notification history interface. Null if unsupported.</summary>
    IToastNotificationHistory? History { get; }

    void Show(IToastNotification notification);
    void Hide(IToastNotification notification);
    NotificationSetting GetSetting();
    void SetExpirationTime(IToastNotification notification, DateTimeOffset expirationTime);
    IScheduledToastNotification CreateScheduledToast(object xmlDoc, DateTimeOffset deliveryTime);
    void AddToSchedule(IScheduledToastNotification scheduled);
    void RemoveFromSchedule(IScheduledToastNotification scheduled);
    NotificationUpdateResult Update(string tag, string? group, Dictionary<string, string> data, uint sequenceNumber);
}

/// <summary>
/// Factory indirection so <see cref="Services.ToastService"/> can inject a mock in tests.
/// </summary>
internal interface INativeToastNotifierFactory
{
    INativeToastNotifier Create(string appId, IWinRtInterop interop);
}

/// <summary>
/// Default factory — instantiates the real <see cref="NativeToastNotifier"/>.
/// </summary>
internal sealed class DefaultNativeToastNotifierFactory : INativeToastNotifierFactory
{
    public static readonly DefaultNativeToastNotifierFactory Instance = new();

    public INativeToastNotifier Create(string appId, IWinRtInterop interop)
        => new NativeToastNotifier(appId, interop);
}
