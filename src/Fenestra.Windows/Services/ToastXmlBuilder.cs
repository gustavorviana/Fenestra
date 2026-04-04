using Fenestra.Windows.Models;
using System.Globalization;
using System.Text;

namespace Fenestra.Windows.Services;

/// <summary>
/// Converts a <see cref="ToastContent"/> into the Windows toast notification XML string.
/// </summary>
internal static class ToastXmlBuilder
{
    public static string Build(ToastContent content, bool useProgressBindings = false)
    {
        var sb = new StringBuilder();

        // <toast>
        sb.Append("<toast");
        if (!string.IsNullOrEmpty(content.LaunchArgs))
            sb.AppendFormat(" launch=\"{0}\"", Escape(content.LaunchArgs));
        if (content.Duration == ToastDuration.Long)
            sb.Append(" duration=\"long\"");
        if (content.Scenario != ToastScenario.Default)
            sb.AppendFormat(" scenario=\"{0}\"", ScenarioToString(content.Scenario));
        if (content.DisplayTimestamp.HasValue)
            sb.AppendFormat(" displayTimestamp=\"{0}\"", content.DisplayTimestamp.Value.ToString("o"));
        if (content.UseButtonStyle)
            sb.Append(" useButtonStyle=\"true\"");
        if (content.ActivationType != ToastActivationType.Foreground)
            sb.AppendFormat(" activationType=\"{0}\"", ActivationToString(content.ActivationType));
        sb.Append(">");

        // <visual>
        sb.Append("<visual><binding template=\"ToastGeneric\">");

        if (!string.IsNullOrEmpty(content.Title))
            sb.AppendFormat("<text>{0}</text>", Escape(content.Title));
        if (!string.IsNullOrEmpty(content.Body))
            sb.AppendFormat("<text>{0}</text>", Escape(content.Body));
        if (!string.IsNullOrEmpty(content.Attribution))
            sb.AppendFormat("<text placement=\"attribution\">{0}</text>", Escape(content.Attribution));

        foreach (var img in content.Images)
        {
            sb.Append("<image");
            sb.AppendFormat(" src=\"{0}\"", Escape(img.Source));
            if (!string.IsNullOrEmpty(img.AltText))
                sb.AppendFormat(" alt=\"{0}\"", Escape(img.AltText));
            if (img.Placement == ToastImagePlacement.AppLogo)
                sb.Append(" placement=\"appLogoOverride\"");
            else if (img.Placement == ToastImagePlacement.Hero)
                sb.Append(" placement=\"hero\"");
            if (img.Crop == ToastImageCrop.Circle)
                sb.Append(" hint-crop=\"circle\"");
            if (img.HintOverlay.HasValue)
                sb.AppendFormat(" hint-overlay=\"{0}\"", img.HintOverlay.Value);
            sb.Append("/>");
        }

        // <group>/<subgroup> adaptive layouts
        foreach (var group in content.Groups)
        {
            sb.Append("<group>");
            foreach (var sub in group.Subgroups)
            {
                sb.Append("<subgroup");
                if (sub.Weight.HasValue)
                    sb.AppendFormat(" hint-weight=\"{0}\"", sub.Weight.Value);
                if (sub.TextStacking != ToastTextStacking.Default)
                    sb.AppendFormat(" hint-textStacking=\"{0}\"", sub.TextStacking.ToString().ToLowerInvariant());
                sb.Append(">");

                foreach (var text in sub.Texts)
                    AppendStyledText(sb, text);

                foreach (var img in sub.Images)
                {
                    sb.Append("<image");
                    sb.AppendFormat(" src=\"{0}\"", Escape(img.Source));
                    if (!string.IsNullOrEmpty(img.AltText))
                        sb.AppendFormat(" alt=\"{0}\"", Escape(img.AltText));
                    if (img.Crop == ToastImageCrop.Circle)
                        sb.Append(" hint-crop=\"circle\"");
                    if (img.HintOverlay.HasValue)
                        sb.AppendFormat(" hint-overlay=\"{0}\"", img.HintOverlay.Value);
                    sb.Append("/>");
                }

                sb.Append("</subgroup>");
            }
            sb.Append("</group>");
        }

        if (content.Progress != null)
        {
            if (useProgressBindings)
            {
                var tracker = content.ProgressTracker;
                sb.Append("<progress");
                sb.Append(" status=\"{progressStatus}\"");
                sb.Append(" value=\"{progressValue}\"");
                if (!string.IsNullOrEmpty(tracker?.Title))
                    sb.Append(" title=\"{progressTitle}\"");
                if (tracker?.UseValueOverride == true)
                    sb.Append(" valueStringOverride=\"{progressValueOverride}\"");
                sb.Append("/>");
            }
            else
            {
                var p = content.Progress;
                sb.Append("<progress");
                if (!string.IsNullOrEmpty(p.Title))
                    sb.AppendFormat(" title=\"{0}\"", Escape(p.Title));
                sb.AppendFormat(" status=\"{0}\"", Escape(p.Status));
                sb.AppendFormat(" value=\"{0}\"", p.Value.HasValue
                    ? p.Value.Value.ToString("F2", CultureInfo.InvariantCulture)
                    : "indeterminate");
                if (!string.IsNullOrEmpty(p.ValueOverride))
                    sb.AppendFormat(" valueStringOverride=\"{0}\"", Escape(p.ValueOverride));
                sb.Append("/>");
            }
        }

        sb.Append("</binding></visual>");

        // <actions>
        if (content.Inputs.Count > 0 || content.Buttons.Count > 0)
        {
            sb.Append("<actions>");

            foreach (var input in content.Inputs)
            {
                sb.AppendFormat("<input id=\"{0}\" type=\"{1}\"",
                    Escape(input.Id),
                    input.Type == ToastInputType.Selection ? "selection" : "text");
                if (!string.IsNullOrEmpty(input.Title))
                    sb.AppendFormat(" title=\"{0}\"", Escape(input.Title));
                if (!string.IsNullOrEmpty(input.Placeholder))
                    sb.AppendFormat(" placeHolderContent=\"{0}\"", Escape(input.Placeholder));
                if (!string.IsNullOrEmpty(input.DefaultValue))
                    sb.AppendFormat(" defaultInput=\"{0}\"", Escape(input.DefaultValue));

                if (input.Type == ToastInputType.Selection && input.Selections.Count > 0)
                {
                    sb.Append(">");
                    foreach (var sel in input.Selections)
                        sb.AppendFormat("<selection id=\"{0}\" content=\"{1}\"/>", Escape(sel.Key), Escape(sel.Value));
                    sb.Append("</input>");
                }
                else
                {
                    sb.Append("/>");
                }
            }

            foreach (var btn in content.Buttons)
            {
                sb.Append("<action");

                if (btn.Type == ToastButtonType.Snooze)
                {
                    sb.Append(" activationType=\"system\" arguments=\"snooze\"");
                    if (!string.IsNullOrEmpty(btn.InputId))
                        sb.AppendFormat(" hint-inputId=\"{0}\"", Escape(btn.InputId));
                    sb.Append(" content=\"\"/>");
                    continue;
                }

                if (btn.Type == ToastButtonType.Dismiss)
                {
                    sb.Append(" activationType=\"system\" arguments=\"dismiss\" content=\"\"/>");
                    continue;
                }

                sb.AppendFormat(" content=\"{0}\"", Escape(btn.Text ?? ""));
                sb.AppendFormat(" arguments=\"{0}\"", Escape(btn.Argument ?? ""));

                if (btn.ActivationType != ToastActivationType.Foreground)
                    sb.AppendFormat(" activationType=\"{0}\"", ActivationToString(btn.ActivationType));
                if (!string.IsNullOrEmpty(btn.ImageUri))
                    sb.AppendFormat(" imageUri=\"{0}\"", Escape(btn.ImageUri));
                if (btn.Style == ToastButtonStyle.Success)
                    sb.Append(" hint-buttonStyle=\"Success\"");
                else if (btn.Style == ToastButtonStyle.Critical)
                    sb.Append(" hint-buttonStyle=\"Critical\"");
                if (!string.IsNullOrEmpty(btn.Tooltip))
                    sb.AppendFormat(" hint-toolTip=\"{0}\"", Escape(btn.Tooltip));
                if (btn.Type == ToastButtonType.ContextMenu)
                    sb.Append(" placement=\"contextMenu\"");
                if (!string.IsNullOrEmpty(btn.InputId))
                    sb.AppendFormat(" hint-inputId=\"{0}\"", Escape(btn.InputId));

                sb.Append("/>");
            }

            sb.Append("</actions>");
        }

        // <audio>
        if (content.Audio != null)
        {
            var a = content.Audio;
            sb.Append("<audio");
            if (a.Silent)
            {
                sb.Append(" silent=\"true\"");
            }
            else
            {
                var src = !string.IsNullOrEmpty(a.CustomUri) ? a.CustomUri : AudioToUri(a.Sound);
                if (src != null)
                    sb.AppendFormat(" src=\"{0}\"", Escape(src));
                if (a.Loop)
                    sb.Append(" loop=\"true\"");
            }
            sb.Append("/>");
        }

        // <header>
        if (content.Header != null)
        {
            var h = content.Header;
            sb.AppendFormat("<header id=\"{0}\" title=\"{1}\" arguments=\"{2}\"",
                Escape(h.Id), Escape(h.Title), Escape(h.Arguments));
            if (h.ActivationType != ToastActivationType.Foreground)
                sb.AppendFormat(" activationType=\"{0}\"", ActivationToString(h.ActivationType));
            sb.Append("/>");
        }

        sb.Append("</toast>");
        return sb.ToString();
    }

    private static string Escape(string? value)
        => (value ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string ScenarioToString(ToastScenario s) => s switch
    {
        ToastScenario.Reminder => "reminder",
        ToastScenario.Alarm => "alarm",
        ToastScenario.IncomingCall => "incomingCall",
        ToastScenario.Urgent => "urgent",
        _ => "default"
    };

    private static string ActivationToString(ToastActivationType t) => t switch
    {
        ToastActivationType.Background => "background",
        ToastActivationType.Protocol => "protocol",
        _ => "foreground"
    };

    private static void AppendStyledText(StringBuilder sb, ToastText text)
    {
        sb.Append("<text");
        if (text.Style != ToastTextStyle.Default)
            sb.AppendFormat(" hint-style=\"{0}\"", TextStyleToString(text.Style));
        if (text.Wrap)
            sb.Append(" hint-wrap=\"true\"");
        if (text.MaxLines.HasValue)
            sb.AppendFormat(" hint-maxLines=\"{0}\"", text.MaxLines.Value);
        if (text.MinLines.HasValue)
            sb.AppendFormat(" hint-minLines=\"{0}\"", text.MinLines.Value);
        if (text.Align != ToastTextAlign.Default)
            sb.AppendFormat(" hint-align=\"{0}\"", text.Align.ToString().ToLowerInvariant());
        sb.AppendFormat(">{0}</text>", Escape(text.Text));
    }

    private static string TextStyleToString(ToastTextStyle style) => style switch
    {
        ToastTextStyle.Caption => "caption",
        ToastTextStyle.CaptionSubtle => "captionSubtle",
        ToastTextStyle.Body => "body",
        ToastTextStyle.BodySubtle => "bodySubtle",
        ToastTextStyle.Base => "base",
        ToastTextStyle.BaseSubtle => "baseSubtle",
        ToastTextStyle.Subtitle => "subtitle",
        ToastTextStyle.SubtitleSubtle => "subtitleSubtle",
        ToastTextStyle.Title => "title",
        ToastTextStyle.TitleSubtle => "titleSubtle",
        ToastTextStyle.Subheader => "subheader",
        ToastTextStyle.SubheaderSubtle => "subheaderSubtle",
        ToastTextStyle.Header => "header",
        ToastTextStyle.HeaderSubtle => "headerSubtle",
        _ => "default"
    };

    private static string? AudioToUri(ToastAudio audio) => audio switch
    {
        ToastAudio.Default => "ms-winsoundevent:Notification.Default",
        ToastAudio.IM => "ms-winsoundevent:Notification.IM",
        ToastAudio.Mail => "ms-winsoundevent:Notification.Mail",
        ToastAudio.Reminder => "ms-winsoundevent:Notification.Reminder",
        ToastAudio.SMS => "ms-winsoundevent:Notification.SMS",
        ToastAudio.Alarm1 => "ms-winsoundevent:Notification.Looping.Alarm",
        ToastAudio.Alarm2 => "ms-winsoundevent:Notification.Looping.Alarm2",
        ToastAudio.Alarm3 => "ms-winsoundevent:Notification.Looping.Alarm3",
        ToastAudio.Alarm4 => "ms-winsoundevent:Notification.Looping.Alarm4",
        ToastAudio.Alarm5 => "ms-winsoundevent:Notification.Looping.Alarm5",
        ToastAudio.Alarm6 => "ms-winsoundevent:Notification.Looping.Alarm6",
        ToastAudio.Alarm7 => "ms-winsoundevent:Notification.Looping.Alarm7",
        ToastAudio.Alarm8 => "ms-winsoundevent:Notification.Looping.Alarm8",
        ToastAudio.Alarm9 => "ms-winsoundevent:Notification.Looping.Alarm9",
        ToastAudio.Alarm10 => "ms-winsoundevent:Notification.Looping.Alarm10",
        ToastAudio.Call1 => "ms-winsoundevent:Notification.Looping.Call",
        ToastAudio.Call2 => "ms-winsoundevent:Notification.Looping.Call2",
        ToastAudio.Call3 => "ms-winsoundevent:Notification.Looping.Call3",
        ToastAudio.Call4 => "ms-winsoundevent:Notification.Looping.Call4",
        ToastAudio.Call5 => "ms-winsoundevent:Notification.Looping.Call5",
        ToastAudio.Call6 => "ms-winsoundevent:Notification.Looping.Call6",
        ToastAudio.Call7 => "ms-winsoundevent:Notification.Looping.Call7",
        ToastAudio.Call8 => "ms-winsoundevent:Notification.Looping.Call8",
        ToastAudio.Call9 => "ms-winsoundevent:Notification.Looping.Call9",
        ToastAudio.Call10 => "ms-winsoundevent:Notification.Looping.Call10",
        _ => null
    };
}
