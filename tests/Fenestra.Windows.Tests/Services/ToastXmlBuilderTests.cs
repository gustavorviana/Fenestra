using Fenestra.Windows;
using Fenestra.Windows.Models;
using Fenestra.Windows.Services;

namespace Fenestra.Windows.Tests.Services;

public class ToastXmlBuilderTests
{
    private static string BuildXml(Action<ToastBuilder> configure, bool useProgressBindings = false)
    {
        var builder = new ToastBuilder();
        configure(builder);
        return ToastXmlBuilder.Build(builder.Build(), useProgressBindings);
    }

    // --- Root element / attributes ---

    [Fact]
    public void Build_EmptyContent_ProducesMinimalToast()
    {
        var xml = ToastXmlBuilder.Build(new ToastContent());

        Assert.Equal("<toast><visual><binding template=\"ToastGeneric\"></binding></visual></toast>", xml);
    }

    [Fact]
    public void Build_WithLaunchArgs_EmitsLaunchAttribute()
    {
        var xml = BuildXml(b => b.Launch("action=open"));

        Assert.Contains("<toast launch=\"action=open\">", xml);
    }

    [Fact]
    public void Build_WithLongDuration_EmitsDurationAttribute()
    {
        var xml = BuildXml(b => b.Duration(ToastDuration.Long));

        Assert.Contains("duration=\"long\"", xml);
    }

    [Fact]
    public void Build_WithShortDuration_DoesNotEmitDuration()
    {
        var xml = BuildXml(b => b.Duration(ToastDuration.Short));

        Assert.DoesNotContain("duration=", xml);
    }

    [Theory]
    [InlineData(ToastScenario.Reminder, "reminder")]
    [InlineData(ToastScenario.Alarm, "alarm")]
    [InlineData(ToastScenario.IncomingCall, "incomingCall")]
    [InlineData(ToastScenario.Urgent, "urgent")]
    public void Build_WithScenario_EmitsScenarioAttribute(ToastScenario scenario, string expected)
    {
        var xml = BuildXml(b => b.Scenario(scenario));

        Assert.Contains($"scenario=\"{expected}\"", xml);
    }

    [Fact]
    public void Build_WithDefaultScenario_DoesNotEmitScenario()
    {
        var xml = BuildXml(b => { });

        Assert.DoesNotContain("scenario=", xml);
    }

    [Fact]
    public void Build_WithDisplayTimestamp_EmitsIso8601Timestamp()
    {
        var timestamp = new DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.Zero);
        var xml = BuildXml(b => b.Timestamp(timestamp));

        Assert.Contains($"displayTimestamp=\"{timestamp:o}\"", xml);
    }

    [Fact]
    public void Build_WithButtonStyles_EmitsUseButtonStyleAttribute()
    {
        var xml = BuildXml(b => b.EnableButtonStyles());

        Assert.Contains("useButtonStyle=\"true\"", xml);
    }

    [Theory]
    [InlineData(ToastActivationType.Background, "background")]
    [InlineData(ToastActivationType.Protocol, "protocol")]
    public void Build_WithNonDefaultActivationType_EmitsActivationType(ToastActivationType type, string expected)
    {
        var xml = BuildXml(b => b.ActivationType(type));

        Assert.Contains($"activationType=\"{expected}\"", xml);
    }

    [Fact]
    public void Build_WithForegroundActivation_DoesNotEmitActivationType()
    {
        var xml = BuildXml(b => b.ActivationType(ToastActivationType.Foreground));

        // Only root toast attributes check — buttons still emit activation
        Assert.DoesNotContain("<toast activationType", xml);
    }

    // --- Text ---

    [Fact]
    public void Build_WithTitle_EmitsTextElement()
    {
        var xml = BuildXml(b => b.Title("Hello"));

        Assert.Contains("<text>Hello</text>", xml);
    }

    [Fact]
    public void Build_WithTitleAndBody_EmitsTwoTextElementsInOrder()
    {
        var xml = BuildXml(b => b.Title("Title").Body("Body text"));

        var titleIdx = xml.IndexOf("<text>Title</text>", StringComparison.Ordinal);
        var bodyIdx = xml.IndexOf("<text>Body text</text>", StringComparison.Ordinal);
        Assert.True(titleIdx >= 0 && bodyIdx > titleIdx, "Title must appear before Body");
    }

    [Fact]
    public void Build_WithAttribution_EmitsAttributionText()
    {
        var xml = BuildXml(b => b.Attribution("via Fenestra"));

        Assert.Contains("<text placement=\"attribution\">via Fenestra</text>", xml);
    }

    // --- Escaping ---

    [Theory]
    [InlineData("a & b", "a &amp; b")]
    [InlineData("<tag>", "&lt;tag&gt;")]
    [InlineData("she said \"hi\"", "she said &quot;hi&quot;")]
    [InlineData("a & b < c > d", "a &amp; b &lt; c &gt; d")]
    public void Build_EscapesSpecialXmlCharactersInText(string input, string expected)
    {
        var xml = BuildXml(b => b.Title(input));

        Assert.Contains($"<text>{expected}</text>", xml);
    }

    [Fact]
    public void Build_EscapesLaunchArgs()
    {
        var xml = BuildXml(b => b.Launch("a&b=\"c\""));

        Assert.Contains("launch=\"a&amp;b=&quot;c&quot;\"", xml);
    }

    // --- Images ---

    [Fact]
    public void Build_InlineImage_EmitsImageWithSrc()
    {
        var xml = BuildXml(b => b.InlineImage("https://example.com/img.png", "alt text"));

        Assert.Contains("<image src=\"https://example.com/img.png\" alt=\"alt text\"/>", xml);
    }

    [Fact]
    public void Build_InlineImageWithoutAlt_OmitsAltAttribute()
    {
        var xml = BuildXml(b => b.InlineImage("img.png"));

        Assert.Contains("<image src=\"img.png\"/>", xml);
    }

    [Fact]
    public void Build_AppLogo_EmitsPlacementAppLogoOverride()
    {
        var xml = BuildXml(b => b.AppLogo("logo.png"));

        Assert.Contains("placement=\"appLogoOverride\"", xml);
    }

    [Fact]
    public void Build_AppLogoWithCircleCrop_EmitsCircleCrop()
    {
        var xml = BuildXml(b => b.AppLogo("logo.png", ToastImageCrop.Circle));

        Assert.Contains("hint-crop=\"circle\"", xml);
    }

    [Fact]
    public void Build_HeroImage_EmitsHeroPlacement()
    {
        var xml = BuildXml(b => b.HeroImage("hero.png"));

        Assert.Contains("placement=\"hero\"", xml);
    }

    // --- Progress (static values) ---

    [Fact]
    public void Build_ProgressWithValue_EmitsFormattedValue()
    {
        var xml = BuildXml(b => b.Progress("Downloading", value: 0.42));

        Assert.Contains("<progress", xml);
        Assert.Contains("status=\"Downloading\"", xml);
        Assert.Contains("value=\"0.42\"", xml);
    }

    [Fact]
    public void Build_ProgressWithoutValue_EmitsIndeterminate()
    {
        var xml = BuildXml(b => b.Progress("Working"));

        Assert.Contains("value=\"indeterminate\"", xml);
    }

    [Fact]
    public void Build_ProgressWithTitle_EmitsTitleAttribute()
    {
        var xml = BuildXml(b => b.Progress("Status", title: "Download"));

        Assert.Contains("title=\"Download\"", xml);
    }

    [Fact]
    public void Build_ProgressWithValueOverride_EmitsValueStringOverride()
    {
        var xml = BuildXml(b => b.Progress("Status", value: 0.5, valueOverride: "50 of 100 MB"));

        Assert.Contains("valueStringOverride=\"50 of 100 MB\"", xml);
    }

    // --- Progress (binding mode for tracker) ---

    [Fact]
    public void Build_WithProgressBindingsAndTracker_EmitsBindingPlaceholders()
    {
        var tracker = new ToastProgressTracker(title: "Upload");
        var content = new ToastBuilder().BindProgress(tracker).Build();

        var xml = ToastXmlBuilder.Build(content, useProgressBindings: true);

        Assert.Contains("status=\"{progressStatus}\"", xml);
        Assert.Contains("value=\"{progressValue}\"", xml);
        Assert.Contains("title=\"{progressTitle}\"", xml);
    }

    [Fact]
    public void Build_WithProgressBindingsAndValueOverride_EmitsValueOverrideBinding()
    {
        var tracker = new ToastProgressTracker(useValueOverride: true);
        var content = new ToastBuilder().BindProgress(tracker).Build();

        var xml = ToastXmlBuilder.Build(content, useProgressBindings: true);

        Assert.Contains("valueStringOverride=\"{progressValueOverride}\"", xml);
    }

    [Fact]
    public void Build_WithProgressBindingsAndNoTitle_OmitsTitleBinding()
    {
        var tracker = new ToastProgressTracker();
        var content = new ToastBuilder().BindProgress(tracker).Build();

        var xml = ToastXmlBuilder.Build(content, useProgressBindings: true);

        Assert.DoesNotContain("title=\"{progressTitle}\"", xml);
    }

    // --- Buttons ---

    [Fact]
    public void Build_WithButton_EmitsActionWithContentAndArguments()
    {
        var xml = BuildXml(b => b.AddButton("OK", "action=ok"));

        Assert.Contains("<actions>", xml);
        Assert.Contains("<action content=\"OK\" arguments=\"action=ok\"/>", xml);
    }

    [Fact]
    public void Build_WithSuccessButton_EmitsSuccessStyle()
    {
        var xml = BuildXml(b => b.AddButton("Accept", "accept", ToastButtonStyle.Success));

        Assert.Contains("hint-buttonStyle=\"Success\"", xml);
    }

    [Fact]
    public void Build_WithCriticalButton_EmitsCriticalStyle()
    {
        var xml = BuildXml(b => b.AddButton("Delete", "delete", ToastButtonStyle.Critical));

        Assert.Contains("hint-buttonStyle=\"Critical\"", xml);
    }

    [Fact]
    public void Build_DismissButton_EmitsSystemActivation()
    {
        var xml = BuildXml(b => b.AddDismissButton());

        Assert.Contains("<action activationType=\"system\" arguments=\"dismiss\" content=\"\"/>", xml);
    }

    [Fact]
    public void Build_SnoozeButtonWithoutInput_EmitsSnoozeAction()
    {
        var xml = BuildXml(b => b.AddSnoozeButton());

        Assert.Contains("activationType=\"system\"", xml);
        Assert.Contains("arguments=\"snooze\"", xml);
    }

    [Fact]
    public void Build_SnoozeButtonWithInputId_EmitsHintInputId()
    {
        var xml = BuildXml(b => b.AddSnoozeButton("snoozeTime"));

        Assert.Contains("hint-inputId=\"snoozeTime\"", xml);
    }

    [Fact]
    public void Build_ContextMenuItem_EmitsContextMenuPlacement()
    {
        var xml = BuildXml(b => b.AddContextMenuItem("Settings", "settings"));

        Assert.Contains("placement=\"contextMenu\"", xml);
    }

    [Fact]
    public void Build_ButtonWithBackgroundActivation_EmitsActivationType()
    {
        var xml = BuildXml(b => b.AddButton(btn =>
            btn.Text("Bg").Argument("bg").ActivationType(ToastActivationType.Background)));

        Assert.Contains("activationType=\"background\"", xml);
    }

    [Fact]
    public void Build_ButtonWithStyleSettingsFlagsUseButtonStyle()
    {
        var xml = BuildXml(b => b.AddButton("Accept", "accept", ToastButtonStyle.Success));

        // Adding a styled button must auto-enable useButtonStyle on the root <toast>
        Assert.Contains("useButtonStyle=\"true\"", xml);
    }

    // --- Inputs ---

    [Fact]
    public void Build_WithTextInput_EmitsInputElement()
    {
        var xml = BuildXml(b => b.AddTextInput("reply", placeholder: "Reply here"));

        Assert.Contains("<input id=\"reply\" type=\"text\"", xml);
        Assert.Contains("placeHolderContent=\"Reply here\"", xml);
    }

    [Fact]
    public void Build_WithSelectionInput_EmitsSelectionChildElements()
    {
        var xml = BuildXml(b => b.AddSelectionInput("duration",
            new Dictionary<string, string> { ["5"] = "5 min", ["10"] = "10 min" }));

        Assert.Contains("<input id=\"duration\" type=\"selection\"", xml);
        Assert.Contains("<selection id=\"5\" content=\"5 min\"/>", xml);
        Assert.Contains("<selection id=\"10\" content=\"10 min\"/>", xml);
    }

    // --- Audio ---

    [Fact]
    public void Build_Silent_EmitsSilentAudio()
    {
        var xml = BuildXml(b => b.Silent());

        Assert.Contains("<audio silent=\"true\"/>", xml);
    }

    [Fact]
    public void Build_DefaultAudio_EmitsDefaultSoundUri()
    {
        var xml = BuildXml(b => b.Audio(ToastAudio.Default));

        Assert.Contains("src=\"ms-winsoundevent:Notification.Default\"", xml);
    }

    [Fact]
    public void Build_AudioLoop_EmitsLoopAttribute()
    {
        var xml = BuildXml(b => b.Audio(ToastAudio.Alarm1).AudioLoop());

        Assert.Contains("loop=\"true\"", xml);
    }

    [Fact]
    public void Build_CustomAudioUri_EmitsCustomSrc()
    {
        var xml = BuildXml(b => b.AudioCustom("ms-appx:///Assets/sound.wav"));

        Assert.Contains("src=\"ms-appx:///Assets/sound.wav\"", xml);
    }

    // --- Header ---

    [Fact]
    public void Build_WithHeader_EmitsHeaderElement()
    {
        var xml = BuildXml(b => b.Header("group1", "My Group", "header-args"));

        Assert.Contains("<header id=\"group1\" title=\"My Group\" arguments=\"header-args\"/>", xml);
    }

    [Fact]
    public void Build_HeaderWithNonDefaultActivation_EmitsActivationType()
    {
        var xml = BuildXml(b => b.Header("id", "Title", "args", ToastActivationType.Background));

        Assert.Contains("activationType=\"background\"", xml);
    }

    // --- Adaptive groups ---

    [Fact]
    public void Build_WithGroupAndSubgroup_EmitsAdaptiveLayout()
    {
        var xml = BuildXml(b => b.AddGroup(g =>
            g.AddSubgroup(sg => sg.Weight(30).AddText("Col A"))
             .AddSubgroup(sg => sg.Weight(70).AddText("Col B"))));

        Assert.Contains("<group>", xml);
        Assert.Contains("hint-weight=\"30\"", xml);
        Assert.Contains("hint-weight=\"70\"", xml);
        Assert.Contains("Col A", xml);
        Assert.Contains("Col B", xml);
        Assert.Contains("</group>", xml);
    }

    [Fact]
    public void Build_SubgroupWithStyledText_EmitsHintStyle()
    {
        var xml = BuildXml(b => b.AddGroup(g =>
            g.AddSubgroup(sg => sg.AddText("Important", ToastTextStyle.Title))));

        Assert.Contains("hint-style=\"title\"", xml);
    }

    [Fact]
    public void Build_SubgroupWithWrappedText_EmitsHintWrap()
    {
        var xml = BuildXml(b => b.AddGroup(g =>
            g.AddSubgroup(sg => sg.AddText("Long text", wrap: true))));

        Assert.Contains("hint-wrap=\"true\"", xml);
    }

    // --- Structure: <actions> omitted when empty ---

    [Fact]
    public void Build_WithoutButtonsOrInputs_OmitsActionsElement()
    {
        var xml = BuildXml(b => b.Title("Hello"));

        Assert.DoesNotContain("<actions>", xml);
    }
}
