using Fenestra.Windows.Services;

namespace Fenestra.Windows.Tests.Services;

/// <summary>
/// Smoke tests for <see cref="TaskbarOverlayService"/>. When no <c>IThreadContext</c>
/// is provided and no main window exists (the default in headless xUnit), the service
/// is a best-effort no-op — which is exactly what these tests verify.
/// </summary>
public class TaskbarOverlayServiceTests
{
    [Fact]
    public void Ctor_DoesNotThrow()
    {
        var ex = Record.Exception(() => new TaskbarOverlayService());

        Assert.Null(ex);
    }

    [Fact]
    public void SetOverlay_WithNullOrMissingPath_IsSilentNoOp()
    {
        var service = new TaskbarOverlayService();

        var ex = Record.Exception(() =>
        {
            service.SetOverlay((string)null!);
            service.SetOverlay("");
            service.SetOverlay("   ");
            service.SetOverlay(@"C:\definitely\does\not\exist.ico");
        });

        Assert.Null(ex);
    }

    [Fact]
    public void SetOverlay_WithZeroHIcon_IsSilentNoOp()
    {
        // IntPtr.Zero is a valid "clear" signal but without a main window handle it's
        // just a no-op.
        var service = new TaskbarOverlayService();

        var ex = Record.Exception(() => service.SetOverlay(IntPtr.Zero));

        Assert.Null(ex);
    }

    [Fact]
    public void Clear_WithoutMainWindow_IsSilentNoOp()
    {
        var service = new TaskbarOverlayService();

        var ex = Record.Exception(() => service.Clear());

        Assert.Null(ex);
    }
}
