using Fenestra.Core;
using Fenestra.Core.Models;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows;

namespace Fenestra.Wpf.Services;

internal class SingleInstanceGuard : IDisposable
{
    private readonly string _pipeName;
    private readonly Mutex _mutex;
    private readonly IServiceProvider _services;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public bool IsFirstInstance { get; }

    public SingleInstanceGuard(AppInfo appInfo, IServiceProvider services)
    {
        _services = services;
        _pipeName = $"Fenestra_{appInfo.AppId}";
        _mutex = new Mutex(true, _pipeName, out var created);
        IsFirstInstance = created;
    }

    public void StartListening()
    {
        if (!IsFirstInstance) return;

        _cts = new CancellationTokenSource();
        Task.Run(() => ListenLoop(_cts.Token));
    }

    public void SendArguments(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
            client.Connect(2000);

            var bytes = Encoding.UTF8.GetBytes(string.Join("\t", args));
            client.Write(bytes, 0, bytes.Length);
            client.Flush();
        }
        catch
        {
            // Best effort
        }
    }

    private async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(token);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

                try
                {
                    var data = await ReadAll(server, timeoutCts.Token);
                    var raw = Encoding.UTF8.GetString(data);
                    var args = string.IsNullOrWhiteSpace(raw) ? [] : raw.Split('\t');
                    DispatchArguments(args);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    // Client took too long — disconnect and accept next
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Pipe error — retry
            }
        }
    }

    private static async Task<byte[]> ReadAll(Stream stream, CancellationToken token)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[1024];

        while (true)
        {
            var readTask = stream.ReadAsync(buffer, 0, buffer.Length, token);
            var read = await readTask;
            if (read == 0) break;
            ms.Write(buffer, 0, read);
        }

        return ms.ToArray();
    }

    private void DispatchArguments(string[] args)
    {
        var handler = _services.GetService(typeof(ISingleInstanceApp)) as ISingleInstanceApp;
        if (handler == null) return;

        Application.Current?.Dispatcher.BeginInvoke(() => handler.OnArgumentsReceived(args));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();

        if (IsFirstInstance)
            _mutex.ReleaseMutex();

        _mutex.Dispose();
    }
}
