namespace Fenestra.Windows.Native;

/// <summary>
/// Abstraction over WinRT activation and COM pointer conversion.
/// Enables dependency injection and testability.
/// </summary>
internal interface IWinRtInterop
{
    IComRef<T>? ActivateInstance<T>(string className) where T : class;
    IComRef<T>? GetActivationFactory<T>(string className, Guid iid) where T : class;
    IComRef<T>? CastPointer<T>(IntPtr pUnk) where T : class;
    IComRef<T>? BorrowPointer<T>(IntPtr pUnk) where T : class;
    void SetCurrentProcessExplicitAppUserModelID(string appID);
}
