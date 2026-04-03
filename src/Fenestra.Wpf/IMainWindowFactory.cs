using System.Windows;

namespace Fenestra.Wpf;

/// <summary>
/// Factory for creating the application's main window.
/// Return null to shut down the application without showing a window.
/// </summary>
public interface IMainWindowFactory
{
    Window? Create();
}
