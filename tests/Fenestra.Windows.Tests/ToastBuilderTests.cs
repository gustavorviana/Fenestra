using Fenestra.Windows;
using Fenestra.Windows.Models;

namespace Fenestra.Windows.Tests;

public class ToastBuilderTests
{
    [Fact]
    public void Build_ReturnsNonNullContent()
    {
        var content = new ToastBuilder().Build();

        Assert.NotNull(content);
    }

    [Fact]
    public void Title_SetsContentTitle()
    {
        var content = new ToastBuilder().Title("Hello").Build();

        Assert.Equal("Hello", content.Title);
    }

    [Fact]
    public void Body_SetsContentBody()
    {
        var content = new ToastBuilder().Body("body text").Build();

        Assert.Equal("body text", content.Body);
    }

    [Fact]
    public void FluentChain_AllSettersReturnSameInstance()
    {
        var builder = new ToastBuilder();

        var result = builder.Title("t").Body("b").Attribution("a").Launch("l");

        Assert.Same(builder, result);
    }

    [Fact]
    public void Duration_PersistsOnContent()
    {
        var content = new ToastBuilder().Duration(ToastDuration.Long).Build();

        Assert.Equal(ToastDuration.Long, content.Duration);
    }

    [Fact]
    public void Scenario_PersistsOnContent()
    {
        var content = new ToastBuilder().Scenario(ToastScenario.Alarm).Build();

        Assert.Equal(ToastScenario.Alarm, content.Scenario);
    }

    [Fact]
    public void Tag_PersistsOnContent()
    {
        var content = new ToastBuilder().Tag("my-tag").Build();

        Assert.Equal("my-tag", content.Tag);
    }

    [Fact]
    public void Group_PersistsOnContent()
    {
        var content = new ToastBuilder().Group("my-group").Build();

        Assert.Equal("my-group", content.Group);
    }

    [Fact]
    public void SuppressPopup_SetsFlag()
    {
        var content = new ToastBuilder().SuppressPopup().Build();

        Assert.True(content.SuppressPopup);
    }

    [Fact]
    public void ExpiresOnReboot_SetsFlag()
    {
        var content = new ToastBuilder().ExpiresOnReboot().Build();

        Assert.True(content.ExpiresOnReboot);
    }

    [Fact]
    public void Priority_PersistsOnContent()
    {
        var content = new ToastBuilder().Priority(ToastPriority.High).Build();

        Assert.Equal(ToastPriority.High, content.Priority);
    }

    [Fact]
    public void ExpirationTime_PersistsOnContent()
    {
        var time = DateTimeOffset.UtcNow.AddHours(1);
        var content = new ToastBuilder().ExpirationTime(time).Build();

        Assert.Equal(time, content.ExpirationTime);
    }

    [Fact]
    public void NotificationMirroring_PersistsOnContent()
    {
        var content = new ToastBuilder().NotificationMirroring(NotificationMirroring.Disabled).Build();

        Assert.Equal(NotificationMirroring.Disabled, content.NotificationMirroring);
    }

    [Fact]
    public void RemoteId_PersistsOnContent()
    {
        var content = new ToastBuilder().RemoteId("remote-1").Build();

        Assert.Equal("remote-1", content.RemoteId);
    }

    // --- Images ---

    [Fact]
    public void InlineImage_AddsImageWithInlinePlacement()
    {
        var content = new ToastBuilder().InlineImage("img.png", "alt").Build();

        var img = Assert.Single(content.Images);
        Assert.Equal("img.png", img.Source);
        Assert.Equal("alt", img.AltText);
        Assert.Equal(ToastImagePlacement.Inline, img.Placement);
    }

    [Fact]
    public void AppLogo_AddsImageWithAppLogoPlacement()
    {
        var content = new ToastBuilder().AppLogo("logo.png", ToastImageCrop.Circle).Build();

        var img = Assert.Single(content.Images);
        Assert.Equal(ToastImagePlacement.AppLogo, img.Placement);
        Assert.Equal(ToastImageCrop.Circle, img.Crop);
    }

    [Fact]
    public void HeroImage_AddsImageWithHeroPlacement()
    {
        var content = new ToastBuilder().HeroImage("hero.png").Build();

        var img = Assert.Single(content.Images);
        Assert.Equal(ToastImagePlacement.Hero, img.Placement);
    }

    // --- Audio ---

    [Fact]
    public void Audio_InitializesAudioConfigWithSound()
    {
        var content = new ToastBuilder().Audio(ToastAudio.IM).Build();

        Assert.NotNull(content.Audio);
        Assert.Equal(ToastAudio.IM, content.Audio!.Sound);
    }

    [Fact]
    public void Silent_SetsSilentFlag()
    {
        var content = new ToastBuilder().Silent().Build();

        Assert.NotNull(content.Audio);
        Assert.True(content.Audio!.Silent);
    }

    [Fact]
    public void AudioLoop_SetsLoopFlag()
    {
        var content = new ToastBuilder().AudioLoop().Build();

        Assert.True(content.Audio!.Loop);
    }

    [Fact]
    public void AudioCustom_SetsCustomUri()
    {
        var content = new ToastBuilder().AudioCustom("ms-appx:///sound.wav").Build();

        Assert.Equal("ms-appx:///sound.wav", content.Audio!.CustomUri);
    }

    [Fact]
    public void MultipleAudioCalls_MergeOnSameConfigInstance()
    {
        var content = new ToastBuilder().Audio(ToastAudio.Mail).AudioLoop().Build();

        Assert.Equal(ToastAudio.Mail, content.Audio!.Sound);
        Assert.True(content.Audio.Loop);
    }

    // --- Buttons ---

    [Fact]
    public void AddButton_SimpleOverload_AddsButton()
    {
        var content = new ToastBuilder().AddButton("OK", "ok-arg").Build();

        var btn = Assert.Single(content.Buttons);
        Assert.Equal("OK", btn.Text);
        Assert.Equal("ok-arg", btn.Argument);
        Assert.Equal(ToastButtonStyle.Default, btn.Style);
    }

    [Fact]
    public void AddButton_StyledSuccess_FlagsUseButtonStyle()
    {
        var content = new ToastBuilder().AddButton("Accept", "accept", ToastButtonStyle.Success).Build();

        Assert.True(content.UseButtonStyle);
    }

    [Fact]
    public void AddButton_StyledCritical_FlagsUseButtonStyle()
    {
        var content = new ToastBuilder().AddButton("Delete", "delete", ToastButtonStyle.Critical).Build();

        Assert.True(content.UseButtonStyle);
    }

    [Fact]
    public void AddButton_DefaultStyle_DoesNotFlagUseButtonStyle()
    {
        var content = new ToastBuilder().AddButton("Plain", "arg").Build();

        Assert.False(content.UseButtonStyle);
    }

    [Fact]
    public void AddButton_WithSubBuilder_ConfiguresAllProperties()
    {
        var content = new ToastBuilder().AddButton(b => b
            .Text("Reply")
            .Argument("reply")
            .ActivationType(ToastActivationType.Background)
            .Icon("icon.png")
            .Tooltip("tooltip text")
            .ForInput("replyInput")).Build();

        var btn = Assert.Single(content.Buttons);
        Assert.Equal("Reply", btn.Text);
        Assert.Equal("reply", btn.Argument);
        Assert.Equal(ToastActivationType.Background, btn.ActivationType);
        Assert.Equal("icon.png", btn.ImageUri);
        Assert.Equal("tooltip text", btn.Tooltip);
        Assert.Equal("replyInput", btn.InputId);
    }

    [Fact]
    public void AddDismissButton_AddsSystemButton()
    {
        var content = new ToastBuilder().AddDismissButton().Build();

        var btn = Assert.Single(content.Buttons);
        Assert.Equal(ToastButtonType.Dismiss, btn.Type);
    }

    [Fact]
    public void AddSnoozeButton_AddsSystemButton()
    {
        var content = new ToastBuilder().AddSnoozeButton().Build();

        var btn = Assert.Single(content.Buttons);
        Assert.Equal(ToastButtonType.Snooze, btn.Type);
    }

    [Fact]
    public void AddSnoozeButton_WithSelectionId_SetsInputId()
    {
        var content = new ToastBuilder().AddSnoozeButton("snoozeSelector").Build();

        var btn = Assert.Single(content.Buttons);
        Assert.Equal("snoozeSelector", btn.InputId);
    }

    [Fact]
    public void AddContextMenuItem_AddsContextMenuButton()
    {
        var content = new ToastBuilder().AddContextMenuItem("Settings", "open-settings").Build();

        var btn = Assert.Single(content.Buttons);
        Assert.Equal(ToastButtonType.ContextMenu, btn.Type);
        Assert.Equal("Settings", btn.Text);
    }

    // --- Inputs ---

    [Fact]
    public void AddTextInput_AddsTextInput()
    {
        var content = new ToastBuilder()
            .AddTextInput("reply", placeholder: "Type...", title: "Message", defaultValue: "hi")
            .Build();

        var input = Assert.Single(content.Inputs);
        Assert.Equal("reply", input.Id);
        Assert.Equal(ToastInputType.Text, input.Type);
        Assert.Equal("Type...", input.Placeholder);
        Assert.Equal("Message", input.Title);
        Assert.Equal("hi", input.DefaultValue);
    }

    [Fact]
    public void AddSelectionInput_AddsSelectionWithOptions()
    {
        var content = new ToastBuilder()
            .AddSelectionInput("duration", new Dictionary<string, string>
            {
                ["5"] = "5 min",
                ["10"] = "10 min"
            })
            .Build();

        var input = Assert.Single(content.Inputs);
        Assert.Equal(ToastInputType.Selection, input.Type);
        Assert.Equal(2, input.Selections.Count);
        Assert.Equal("5 min", input.Selections["5"]);
        Assert.Equal("10 min", input.Selections["10"]);
    }

    // --- Progress ---

    [Fact]
    public void Progress_SetsProgressModel()
    {
        var content = new ToastBuilder().Progress("Uploading", value: 0.5, title: "File", valueOverride: "50%").Build();

        Assert.NotNull(content.Progress);
        Assert.Equal("Uploading", content.Progress!.Status);
        Assert.Equal(0.5, content.Progress.Value);
        Assert.Equal("File", content.Progress.Title);
        Assert.Equal("50%", content.Progress.ValueOverride);
    }

    [Fact]
    public void BindProgress_SetsTrackerAndPlaceholderProgress()
    {
        var tracker = new ToastProgressTracker(title: "Upload");
        var content = new ToastBuilder().BindProgress(tracker).Build();

        Assert.Same(tracker, content.ProgressTracker);
        Assert.NotNull(content.Progress);
    }

    [Fact]
    public void BindProgress_AutoAssignsTagIfMissing()
    {
        var content = new ToastBuilder().BindProgress(new ToastProgressTracker()).Build();

        Assert.NotNull(content.Tag);
        Assert.StartsWith("progress-", content.Tag);
    }

    [Fact]
    public void BindProgress_PreservesExistingTag()
    {
        var content = new ToastBuilder().Tag("my-tag").BindProgress(new ToastProgressTracker()).Build();

        Assert.Equal("my-tag", content.Tag);
    }

    // --- Adaptive groups ---

    [Fact]
    public void AddGroup_WithSubgroupAndText_BuildsStructure()
    {
        var content = new ToastBuilder().AddGroup(g =>
            g.AddSubgroup(sg => sg.Weight(50).AddText("Hello"))).Build();

        var group = Assert.Single(content.Groups);
        var sub = Assert.Single(group.Subgroups);
        Assert.Equal(50, sub.Weight);
        var text = Assert.Single(sub.Texts);
        Assert.Equal("Hello", text.Text);
    }

    // --- Header ---

    [Fact]
    public void Header_SetsHeaderOnContent()
    {
        var content = new ToastBuilder().Header("h1", "Group", "args").Build();

        Assert.NotNull(content.Header);
        Assert.Equal("h1", content.Header!.Id);
        Assert.Equal("Group", content.Header.Title);
        Assert.Equal("args", content.Header.Arguments);
    }
}
