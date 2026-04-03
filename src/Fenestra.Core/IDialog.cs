namespace Fenestra.Core;

/// <summary>
/// Marker interface for modal dialog windows.
/// </summary>
public interface IDialog
{
    /// <summary>
    /// Shows the dialog modally and returns the dialog result.
    /// </summary>
    bool? ShowDialog();

    /// <summary>
    /// Shows the dialog as a non-modal window.
    /// </summary>
    void Show();

    /// <summary>
    /// Closes the dialog window.
    /// </summary>
    void Close();

    /// <summary>
    /// Raised when the dialog is closed.
    /// </summary>
    event EventHandler Closed;
}

/// <summary>
/// Modal dialog window that returns a typed result.
/// </summary>
public interface IDialog<TResult> : IDialog
{
    /// <summary>
    /// Gets or sets the typed result returned by the dialog.
    /// </summary>
    TResult? Result { get; set; }
}