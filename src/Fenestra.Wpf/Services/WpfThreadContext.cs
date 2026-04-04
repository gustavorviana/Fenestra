using System.Windows;
using Fenestra.Core;

namespace Fenestra.Wpf.Services;

/// <summary>
/// WPF <see cref="IThreadContext"/> implementation using the application Dispatcher.
/// </summary>
internal class WpfThreadContext : IThreadContext
{
    /// <inheritdoc />
    public void Invoke(Action action)
    {
        var app = Application.Current;
        if (app == null)
        {
            action();
            return;
        }

        app.Dispatcher.Invoke(action);
    }

    /// <inheritdoc />
    public Task InvokeAsync(Action action)
    {
        var app = Application.Current;
        if (app == null)
        {
            action();
            return Task.CompletedTask;
        }

        return app.Dispatcher.InvokeAsync(action).Task;
    }
}
