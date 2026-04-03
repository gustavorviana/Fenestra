using System.IO;
using System.IO.Pipes;
using System.Windows;
using Fenestra.Core;
using Fenestra.Core.Models;

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
        Task.Run(() => ListenForArguments(_cts.Token));
    }

    public void SendArguments(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
            client.Connect(2000);

            using var writer = new StreamWriter(client);
            writer.WriteLine(args.Length.ToString());
            foreach (var arg in args)
                writer.WriteLine(arg);
            writer.Flush();
        }
        catch
        {
            // Best effort — first instance may not be listening yet
        }
    }

    private async Task ListenForArguments(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(token);

                using var reader = new StreamReader(server);
                var countLine = await reader.ReadLineAsync();
                if (int.TryParse(countLine, out var count))
                {
                    var args = new string[count];
                    for (int i = 0; i < count; i++)
                        args[i] = await reader.ReadLineAsync() ?? string.Empty;

                    DispatchArguments(args);
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
