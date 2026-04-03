using Fenestra.Core.Models;

namespace Fenestra.Sample.AppStyle;

public record ShowWindowEvent;
public record ThemeChangedEvent(TrayMenuTheme? Theme, string? CustomBackground = null);
public record BadgeChangedEvent(int? Quantity = null, bool IsDot = false, bool IsClear = false);
public record AnimationToggleEvent(bool Start);
public record BalloonRequestEvent(string Title, string Text, TrayBalloonIcon Icon = TrayBalloonIcon.Info);
public record AutoStartToggleEvent(bool Enable);
public record ExitRequestEvent;
