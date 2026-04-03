namespace Fenestra.Core;

public interface IWindowManager
{
    T Show<T>() where T : class;
    T Show<T>(object dataContext) where T : class;

    bool ShowDialog<T>() where T : class, IDialog;
    bool ShowDialog<T>(object dataContext) where T : class, IDialog;

    TResult ShowDialog<T, TResult>() where T : class, IDialog<TResult>;
    TResult ShowDialog<T, TResult>(object dataContext) where T : class, IDialog<TResult>;

    T? GetOpenWindow<T>() where T : class;
    void Close<T>() where T : class;
    void CloseAllDialogs();
}
