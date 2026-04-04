using Fenestra.Windows.Models;

namespace Fenestra.Windows;

/// <summary>
/// Fluent builder for constructing toast notification content.
/// </summary>
public class ToastBuilder
{
    private readonly ToastContent _content = new();

    // --- Text ---

    /// <summary>
    /// Sets the title (first line of text).
    /// </summary>
    public ToastBuilder Title(string title) { _content.Title = title; return this; }

    /// <summary>
    /// Sets the body text (second line).
    /// </summary>
    public ToastBuilder Body(string body) { _content.Body = body; return this; }

    /// <summary>
    /// Sets attribution text displayed at the bottom.
    /// </summary>
    public ToastBuilder Attribution(string text) { _content.Attribution = text; return this; }

    // --- Behavior ---

    /// <summary>
    /// Sets the arguments passed when the toast body is clicked.
    /// </summary>
    public ToastBuilder Launch(string args) { _content.LaunchArgs = args; return this; }

    /// <summary>
    /// Sets the activation type for the toast body click.
    /// </summary>
    public ToastBuilder ActivationType(ToastActivationType type) { _content.ActivationType = type; return this; }

    /// <summary>
    /// Sets the display duration.
    /// </summary>
    public ToastBuilder Duration(ToastDuration duration) { _content.Duration = duration; return this; }

    /// <summary>
    /// Sets the toast scenario (Reminder, Alarm, IncomingCall, Urgent).
    /// </summary>
    public ToastBuilder Scenario(ToastScenario scenario) { _content.Scenario = scenario; return this; }

    /// <summary>
    /// Sets a custom timestamp displayed in Action Center.
    /// </summary>
    public ToastBuilder Timestamp(DateTimeOffset timestamp) { _content.DisplayTimestamp = timestamp; return this; }

    /// <summary>
    /// Enables colored button styles (Success/Critical).
    /// </summary>
    public ToastBuilder EnableButtonStyles() { _content.UseButtonStyle = true; return this; }

    /// <summary>
    /// Sets a tag for identifying this toast (for updates and removal).
    /// </summary>
    public ToastBuilder Tag(string tag) { _content.Tag = tag; return this; }

    /// <summary>
    /// Sets a group for this toast (for grouping and bulk removal).
    /// </summary>
    public ToastBuilder Group(string group) { _content.Group = group; return this; }

    /// <summary>
    /// Suppresses the popup, sending the toast directly to Action Center.
    /// </summary>
    public ToastBuilder SuppressPopup() { _content.SuppressPopup = true; return this; }

    /// <summary>
    /// Sets the toast priority.
    /// </summary>
    public ToastBuilder Priority(ToastPriority priority) { _content.Priority = priority; return this; }

    /// <summary>
    /// Marks the toast to be removed from Action Center on reboot.
    /// </summary>
    public ToastBuilder ExpiresOnReboot() { _content.ExpiresOnReboot = true; return this; }

    /// <summary>
    /// Sets the expiration time after which the toast is removed from Action Center.
    /// </summary>
    public ToastBuilder ExpirationTime(DateTimeOffset expiration) { _content.ExpirationTime = expiration; return this; }

    // --- Images ---

    /// <summary>
    /// Adds an inline image (full width).
    /// </summary>
    public ToastBuilder InlineImage(string source, string? alt = null)
    {
        _content.Images.Add(new ToastImage { Source = source, AltText = alt, Placement = ToastImagePlacement.Inline });
        return this;
    }

    /// <summary>
    /// Sets the app logo image (small square or circle on the left).
    /// </summary>
    public ToastBuilder AppLogo(string source, ToastImageCrop crop = ToastImageCrop.Default, string? alt = null)
    {
        _content.Images.Add(new ToastImage { Source = source, AltText = alt, Placement = ToastImagePlacement.AppLogo, Crop = crop });
        return this;
    }

    /// <summary>
    /// Sets the hero image (wide banner at the top).
    /// </summary>
    public ToastBuilder HeroImage(string source, string? alt = null)
    {
        _content.Images.Add(new ToastImage { Source = source, AltText = alt, Placement = ToastImagePlacement.Hero });
        return this;
    }

    // --- Audio ---

    /// <summary>
    /// Sets the notification sound.
    /// </summary>
    public ToastBuilder Audio(ToastAudio sound)
    {
        EnsureAudio().Sound = sound;
        return this;
    }

    /// <summary>
    /// Sets a custom audio URI.
    /// </summary>
    public ToastBuilder AudioCustom(string uri)
    {
        EnsureAudio().CustomUri = uri;
        return this;
    }

    /// <summary>
    /// Enables looping audio (requires Duration = Long).
    /// </summary>
    public ToastBuilder AudioLoop()
    {
        EnsureAudio().Loop = true;
        return this;
    }

    /// <summary>
    /// Suppresses all notification sound.
    /// </summary>
    public ToastBuilder Silent()
    {
        EnsureAudio().Silent = true;
        return this;
    }

    // --- Header ---

    /// <summary>
    /// Sets a header for grouping notifications in Action Center.
    /// </summary>
    public ToastBuilder Header(string id, string title, string arguments, ToastActivationType activationType = Models.ToastActivationType.Foreground)
    {
        _content.Header = new ToastHeader { Id = id, Title = title, Arguments = arguments, ActivationType = activationType };
        return this;
    }

    // --- Progress ---

    /// <summary>
    /// Adds a progress bar with static values.
    /// </summary>
    public ToastBuilder Progress(string status, double? value = null, string? title = null, string? valueOverride = null)
    {
        _content.Progress = new ToastProgress { Status = status, Value = value, Title = title, ValueOverride = valueOverride };
        return this;
    }

    /// <summary>
    /// Binds a progress tracker that automatically updates the toast when <see cref="ToastProgressTracker.Report"/> is called.
    /// </summary>
    public ToastBuilder BindProgress(ToastProgressTracker tracker)
    {
        _content.ProgressTracker = tracker;
        _content.Progress = new ToastProgress { Status = " " }; // placeholder — will use binding syntax
        if (string.IsNullOrEmpty(_content.Tag))
            _content.Tag = $"progress-{Guid.NewGuid():N}";
        return this;
    }

    // --- Buttons ---

    /// <summary>
    /// Adds a simple action button.
    /// </summary>
    public ToastBuilder AddButton(string text, string argument, ToastButtonStyle style = ToastButtonStyle.Default)
    {
        if (style != ToastButtonStyle.Default) _content.UseButtonStyle = true;
        _content.Buttons.Add(new ToastButton { Text = text, Argument = argument, Style = style });
        return this;
    }

    /// <summary>
    /// Adds a button configured via a sub-builder.
    /// </summary>
    public ToastBuilder AddButton(Action<ToastButtonBuilder> configure)
    {
        var builder = new ToastButtonBuilder();
        configure(builder);
        var button = builder.Build();
        if (button.Style != ToastButtonStyle.Default) _content.UseButtonStyle = true;
        _content.Buttons.Add(button);
        return this;
    }

    /// <summary>
    /// Adds a context menu item (shown on right-click).
    /// </summary>
    public ToastBuilder AddContextMenuItem(string text, string argument)
    {
        _content.Buttons.Add(new ToastButton { Text = text, Argument = argument, Type = ToastButtonType.ContextMenu });
        return this;
    }

    /// <summary>
    /// Adds a system snooze button, optionally tied to a selection input for snooze duration.
    /// </summary>
    public ToastBuilder AddSnoozeButton(string? selectionInputId = null)
    {
        _content.Buttons.Add(new ToastButton { Type = ToastButtonType.Snooze, InputId = selectionInputId });
        return this;
    }

    /// <summary>
    /// Adds a system dismiss button.
    /// </summary>
    public ToastBuilder AddDismissButton()
    {
        _content.Buttons.Add(new ToastButton { Type = ToastButtonType.Dismiss });
        return this;
    }

    // --- Inputs ---

    /// <summary>
    /// Adds a text input field.
    /// </summary>
    public ToastBuilder AddTextInput(string id, string? placeholder = null, string? title = null, string? defaultValue = null)
    {
        _content.Inputs.Add(new ToastInput
        {
            Id = id,
            Type = ToastInputType.Text,
            Title = title,
            Placeholder = placeholder,
            DefaultValue = defaultValue
        });
        return this;
    }

    /// <summary>
    /// Adds a selection (dropdown) input.
    /// </summary>
    public ToastBuilder AddSelectionInput(string id, Dictionary<string, string> selections, string? title = null, string? defaultValue = null)
    {
        var input = new ToastInput
        {
            Id = id,
            Type = ToastInputType.Selection,
            Title = title,
            DefaultValue = defaultValue
        };
        foreach (var kv in selections)
            input.Selections[kv.Key] = kv.Value;
        _content.Inputs.Add(input);
        return this;
    }

    // --- Adaptive Layout ---

    /// <summary>
    /// Adds an adaptive group with subgroups for multi-column layout.
    /// </summary>
    public ToastBuilder AddGroup(Action<ToastGroupBuilder> configure)
    {
        var builder = new ToastGroupBuilder();
        configure(builder);
        _content.Groups.Add(builder.Build());
        return this;
    }

    // --- Build ---

    /// <summary>
    /// Builds and returns the toast content.
    /// </summary>
    public ToastContent Build() => _content;

    private ToastAudioConfig EnsureAudio() => _content.Audio ??= new ToastAudioConfig();
}

/// <summary>
/// Sub-builder for configuring a toast action button.
/// </summary>
public class ToastButtonBuilder
{
    private readonly ToastButton _button = new();

    /// <summary>Sets the button text.</summary>
    public ToastButtonBuilder Text(string text) { _button.Text = text; return this; }

    /// <summary>Sets the activation argument.</summary>
    public ToastButtonBuilder Argument(string arg) { _button.Argument = arg; return this; }

    /// <summary>Sets the activation type.</summary>
    public ToastButtonBuilder ActivationType(ToastActivationType type) { _button.ActivationType = type; return this; }

    /// <summary>Sets a button icon URI.</summary>
    public ToastButtonBuilder Icon(string uri) { _button.ImageUri = uri; return this; }

    /// <summary>Sets the button visual style.</summary>
    public ToastButtonBuilder Style(ToastButtonStyle style) { _button.Style = style; return this; }

    /// <summary>Sets tooltip text (Windows 11+).</summary>
    public ToastButtonBuilder Tooltip(string text) { _button.Tooltip = text; return this; }

    /// <summary>Associates this button with an input field (quick reply pattern).</summary>
    public ToastButtonBuilder ForInput(string inputId) { _button.InputId = inputId; return this; }

    internal ToastButton Build() => _button;
}

/// <summary>
/// Sub-builder for configuring an adaptive toast group.
/// </summary>
public class ToastGroupBuilder
{
    private readonly ToastGroup _group = new();

    /// <summary>Adds a subgroup (column) to this group.</summary>
    public ToastGroupBuilder AddSubgroup(Action<ToastSubgroupBuilder> configure)
    {
        var builder = new ToastSubgroupBuilder();
        configure(builder);
        _group.Subgroups.Add(builder.Build());
        return this;
    }

    internal ToastGroup Build() => _group;
}

/// <summary>
/// Sub-builder for configuring a subgroup (column) within an adaptive group.
/// </summary>
public class ToastSubgroupBuilder
{
    private readonly ToastSubgroup _subgroup = new();

    /// <summary>Sets the relative weight (width) of this subgroup.</summary>
    public ToastSubgroupBuilder Weight(int weight) { _subgroup.Weight = weight; return this; }

    /// <summary>Sets the vertical text stacking alignment.</summary>
    public ToastSubgroupBuilder TextStacking(ToastTextStacking stacking) { _subgroup.TextStacking = stacking; return this; }

    /// <summary>Adds a styled text element.</summary>
    public ToastSubgroupBuilder AddText(string text, ToastTextStyle style = ToastTextStyle.Default,
        bool wrap = false, int? maxLines = null, int? minLines = null, ToastTextAlign align = ToastTextAlign.Default)
    {
        _subgroup.Texts.Add(new ToastText
        {
            Text = text, Style = style, Wrap = wrap, MaxLines = maxLines, MinLines = minLines, Align = align
        });
        return this;
    }

    /// <summary>Adds an image to this subgroup.</summary>
    public ToastSubgroupBuilder AddImage(string source, string? alt = null, ToastImageCrop crop = ToastImageCrop.Default, int? hintOverlay = null)
    {
        _subgroup.Images.Add(new ToastImage
        {
            Source = source, AltText = alt, Crop = crop, HintOverlay = hintOverlay
        });
        return this;
    }

    internal ToastSubgroup Build() => _subgroup;
}
