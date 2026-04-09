using Fenestra.Windows;

namespace Fenestra.Windows.Tests;

public class ToastProgressTrackerTests
{
    [Fact]
    public void Ctor_DefaultsValueToZero()
    {
        var tracker = new ToastProgressTracker();

        Assert.Equal(0, tracker.Value);
    }

    [Fact]
    public void Ctor_StoresTitleAndUseValueOverride()
    {
        var tracker = new ToastProgressTracker("Upload", useValueOverride: true);

        Assert.Equal("Upload", tracker.Title);
        Assert.True(tracker.UseValueOverride);
    }

    [Fact]
    public void Report_Value_UpdatesValueProperty()
    {
        var tracker = new ToastProgressTracker();

        tracker.Report(0.75);

        Assert.Equal(0.75, tracker.Value);
    }

    [Fact]
    public void Report_WithoutBinding_DoesNotThrow()
    {
        var tracker = new ToastProgressTracker();

        var ex = Record.Exception(() => tracker.Report(0.5, "Status"));

        Assert.Null(ex);
    }

    [Fact]
    public void Report_Value_InvokesCallbackWithProgressValueKey()
    {
        var tracker = new ToastProgressTracker();
        Dictionary<string, string>? received = null;
        tracker.Bind(data => received = data);

        tracker.Report(0.25);

        Assert.NotNull(received);
        Assert.Equal("0.25", received!["progressValue"]);
    }

    [Fact]
    public void Report_Value_FormatsWithInvariantCulture()
    {
        var tracker = new ToastProgressTracker();
        Dictionary<string, string>? received = null;
        tracker.Bind(data => received = data);

        var previousCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("pt-BR");
            tracker.Report(0.42);

            // Must use dot, not comma, regardless of culture
            Assert.Equal("0.42", received!["progressValue"]);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void Report_Status_InvokesCallbackWithProgressStatusKey()
    {
        var tracker = new ToastProgressTracker();
        Dictionary<string, string>? received = null;
        tracker.Bind(data => received = data);

        tracker.Report("Downloading");

        Assert.Equal("Downloading", received!["progressStatus"]);
    }

    [Fact]
    public void Report_Status_DoesNotIncludeValueKey()
    {
        var tracker = new ToastProgressTracker();
        Dictionary<string, string>? received = null;
        tracker.Bind(data => received = data);

        tracker.Report("Status only");

        Assert.False(received!.ContainsKey("progressValue"));
    }

    [Fact]
    public void Report_ValueAndStatus_IncludesBoth()
    {
        var tracker = new ToastProgressTracker();
        Dictionary<string, string>? received = null;
        tracker.Bind(data => received = data);

        tracker.Report(0.5, "Halfway");

        Assert.Equal("0.50", received!["progressValue"]);
        Assert.Equal("Halfway", received["progressStatus"]);
    }

    [Fact]
    public void Report_WithTrackerTitle_IncludesTitleInCallback()
    {
        var tracker = new ToastProgressTracker("Upload");
        Dictionary<string, string>? received = null;
        tracker.Bind(data => received = data);

        tracker.Report(0.5);

        Assert.Equal("Upload", received!["progressTitle"]);
    }

    [Fact]
    public void Report_WithoutTrackerTitle_OmitsTitleFromCallback()
    {
        var tracker = new ToastProgressTracker();
        Dictionary<string, string>? received = null;
        tracker.Bind(data => received = data);

        tracker.Report(0.5);

        Assert.False(received!.ContainsKey("progressTitle"));
    }

    [Fact]
    public void Report_ValueOverride_WhenUseValueOverrideFalse_IsIgnored()
    {
        var tracker = new ToastProgressTracker(useValueOverride: false);
        Dictionary<string, string>? received = null;
        tracker.Bind(data => received = data);

        tracker.Report(0.5, "Status", "50 of 100");

        Assert.False(received!.ContainsKey("progressValueOverride"));
    }

    [Fact]
    public void Report_ValueOverride_WhenUseValueOverrideTrue_IsIncluded()
    {
        var tracker = new ToastProgressTracker(useValueOverride: true);
        Dictionary<string, string>? received = null;
        tracker.Bind(data => received = data);

        tracker.Report(0.5, "Status", "50 of 100");

        Assert.Equal("50 of 100", received!["progressValueOverride"]);
    }

    [Fact]
    public void Report_ValueWithoutStatus_OmitsStatusKey()
    {
        var tracker = new ToastProgressTracker();
        Dictionary<string, string>? received = null;
        tracker.Bind(data => received = data);

        tracker.Report(0.5);

        Assert.False(received!.ContainsKey("progressStatus"));
    }

    [Fact]
    public void Bind_ReplacesPreviousCallback()
    {
        var tracker = new ToastProgressTracker();
        var first = 0;
        var second = 0;
        tracker.Bind(_ => first++);
        tracker.Bind(_ => second++);

        tracker.Report(0.5);

        Assert.Equal(0, first);
        Assert.Equal(1, second);
    }
}
