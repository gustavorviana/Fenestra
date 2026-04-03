namespace Fenestra.Core;

public interface IDialog
{
    bool? ShowDialog();
    void Show();
    void Close();
    event EventHandler Closed;
}

public interface IDialog<TResult> : IDialog
{
    TResult? Result { get; set; }
}