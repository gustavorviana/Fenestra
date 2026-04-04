using Fenestra.Core;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Fenestra.Wpf.Services;

/// <summary>
/// Watches a registry key for value changes and invokes a callback when a change is detected.
/// </summary>
internal class RegistryWatcher : FenestraComponent
{
    private const int REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;

    private readonly string _keyPath;
    private readonly Action _onChanged;
    private CancellationTokenSource? _cts;
    private Thread? _thread;

    /// <summary>
    /// Creates a watcher for the specified registry key under HKEY_CURRENT_USER.
    /// </summary>
    public RegistryWatcher(string keyPath, Action onChanged)
    {
        _keyPath = keyPath;
        _onChanged = onChanged;
    }

    /// <summary>
    /// Starts watching the registry key for changes.
    /// </summary>
    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        _thread = new Thread(Watch) { IsBackground = true, Name = $"RegistryWatcher:{_keyPath}" };
        _thread.Start();
    }

    /// <summary>
    /// Stops watching the registry key.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _thread = null;
    }

    private void Watch()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(_keyPath, false);
            if (key == null || _cts == null) return;

            var hKey = key.Handle.DangerousGetHandle();

            while (!_cts.IsCancellationRequested)
            {
                var waitHandle = new ManualResetEvent(false);
                var hEvent = waitHandle.SafeWaitHandle.DangerousGetHandle();

                int result = RegNotifyChangeKeyValue(hKey, false, REG_NOTIFY_CHANGE_LAST_SET, hEvent, true);
                if (result != 0) { waitHandle.Dispose(); break; }

                WaitHandle.WaitAny([waitHandle, _cts.Token.WaitHandle], 30000);
                waitHandle.Dispose();

                if (_cts.IsCancellationRequested) break;

                _onChanged();
            }
        }
        catch
        {
            // Registry watch failed — stop silently
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Stop();
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegNotifyChangeKeyValue(
        IntPtr hKey, bool bWatchSubtree, int dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);
}
