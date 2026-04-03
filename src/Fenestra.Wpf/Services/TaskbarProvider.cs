using System.Windows;
using Fenestra.Core;

namespace Fenestra.Wpf.Services;

internal class TaskbarProvider : ITaskbarProvider
{
    private readonly object _lock = new();
    private readonly Dictionary<Window, TaskbarProgress> _active = new();

    public ITaskbarProgress Create(object window)
    {
        if (window is not Window wpfWindow)
            throw new ArgumentException("Expected a WPF Window instance.", nameof(window));

        lock (_lock)
        {
            if (_active.TryGetValue(wpfWindow, out var existing) && !existing.IsDisposed)
                throw new InvalidOperationException("A taskbar progress is already active for this window. Dispose the current instance before creating a new one.");

            var progress = new TaskbarProgress(wpfWindow, this);
            _active[wpfWindow] = progress;
            return progress;
        }
    }

    internal void OnDisposed(TaskbarProgress progress, Window window)
    {
        lock (_lock)
            if (_active.TryGetValue(window, out var current) && current == progress)
                _active.Remove(window);
    }
}
