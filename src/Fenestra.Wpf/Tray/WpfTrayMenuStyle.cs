using Fenestra.Core.Drawing;
using Fenestra.Core.Tray;
using Fenestra.Wpf.Extensions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Fenestra.Wpf.Tray;

/// <summary>
/// WPF implementation of tray menu styling with custom templates and hover highlights.
/// </summary>
public class WpfTrayMenuStyle : TrayMenuStyle
{
    protected override void OnApplyTheme(object menu, TrayMenuColors colors, double cornerRadius)
    {
        if (menu is not ContextMenu ctx) return;

        var background = colors.Background?.ToBrush() ?? SystemColors.MenuBrush;
        var foreground = colors.Foreground?.ToBrush();
        var borderBrush = colors.Border?.ToBrush() ?? new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
        var separatorBrush = colors.Separator?.ToBrush() ?? new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));

        ctx.Background = background;
        ctx.HasDropShadow = true;

        // ContextMenu ControlTemplate
        var template = new ControlTemplate(typeof(ContextMenu));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.BackgroundProperty, background);
        borderFactory.SetValue(Border.BorderBrushProperty, borderBrush);
        borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(4));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));

        var presenter = new FrameworkElementFactory(typeof(ItemsPresenter));
        borderFactory.AppendChild(presenter);
        template.VisualTree = borderFactory;
        ctx.Template = template;

        // Compute hover brush
        var bgColor = (background as SolidColorBrush)?.Color ?? Colors.Gray;
        double luminance = (0.299 * bgColor.R + 0.587 * bgColor.G + 0.114 * bgColor.B) / 255.0;
        byte offset = 25;
        var hoverColor = luminance < 0.5
            ? Color.FromRgb((byte)Math.Min(bgColor.R + offset, 255), (byte)Math.Min(bgColor.G + offset, 255), (byte)Math.Min(bgColor.B + offset, 255))
            : Color.FromRgb((byte)Math.Max(bgColor.R - offset, 0), (byte)Math.Max(bgColor.G - offset, 0), (byte)Math.Max(bgColor.B - offset, 0));
        var hoverBrush = new SolidColorBrush(hoverColor);

        // SystemColors overrides
        ctx.Resources[SystemColors.MenuBrushKey] = background;
        ctx.Resources[SystemColors.HighlightBrushKey] = hoverBrush;
        if (foreground != null)
        {
            ctx.Resources[SystemColors.MenuTextBrushKey] = foreground;
            ctx.Resources[SystemColors.HighlightTextBrushKey] = foreground;
        }

        // MenuItem Style with custom ControlTemplates per role
        var menuItemStyle = new Style(typeof(MenuItem));
        if (foreground != null)
            menuItemStyle.Setters.Add(new Setter(Control.ForegroundProperty, foreground));

        var submenuItemTrigger = new Trigger { Property = MenuItem.RoleProperty, Value = MenuItemRole.SubmenuItem };
        submenuItemTrigger.Setters.Add(new Setter(Control.TemplateProperty,
            CreateMenuItemTemplate(background, hoverBrush, foreground, isSubmenuHeader: false)));
        menuItemStyle.Triggers.Add(submenuItemTrigger);

        var submenuHeaderTrigger = new Trigger { Property = MenuItem.RoleProperty, Value = MenuItemRole.SubmenuHeader };
        submenuHeaderTrigger.Setters.Add(new Setter(Control.TemplateProperty,
            CreateMenuItemTemplate(background, hoverBrush, foreground, isSubmenuHeader: true)));
        menuItemStyle.Triggers.Add(submenuHeaderTrigger);

        // Separator style
        var separatorStyle = new Style(typeof(Separator));
        separatorStyle.Setters.Add(new Setter(Separator.BackgroundProperty, separatorBrush));
        separatorStyle.Setters.Add(new Setter(Separator.MarginProperty, new Thickness(4, 2, 4, 2)));

        ctx.Resources[typeof(MenuItem)] = menuItemStyle;
        ctx.Resources[typeof(Separator)] = separatorStyle;
    }

    private static ControlTemplate CreateMenuItemTemplate(
        Brush background, Brush hoverBrush, Brush? foreground, bool isSubmenuHeader)
    {
        var tmpl = new ControlTemplate(typeof(MenuItem));

        var bd = new FrameworkElementFactory(typeof(Border), "Bd");
        bd.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        bd.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        bd.SetValue(Border.BorderThicknessProperty, new Thickness(0));
        bd.SetValue(Border.PaddingProperty, new Thickness(6, 3, 6, 3));
        bd.SetValue(Border.SnapsToDevicePixelsProperty, true);

        var grid = new FrameworkElementFactory(typeof(Grid));
        var col0 = new FrameworkElementFactory(typeof(ColumnDefinition));
        col0.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Auto));
        col0.SetValue(ColumnDefinition.SharedSizeGroupProperty, "MenuItemIconColumnGroup");
        var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
        col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
        var col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
        col2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Auto));
        col2.SetValue(ColumnDefinition.SharedSizeGroupProperty, "MenuItemIGTColumnGroup");
        grid.AppendChild(col0);
        grid.AppendChild(col1);
        grid.AppendChild(col2);

        if (isSubmenuHeader)
        {
            var col3 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col3.SetValue(ColumnDefinition.WidthProperty, new GridLength(14, GridUnitType.Pixel));
            grid.AppendChild(col3);
        }

        var icon = new FrameworkElementFactory(typeof(ContentPresenter), "Icon");
        icon.SetValue(ContentPresenter.ContentSourceProperty, "Icon");
        icon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 6, 0));
        icon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        icon.SetValue(FrameworkElement.WidthProperty, 16.0);
        icon.SetValue(FrameworkElement.HeightProperty, 16.0);
        grid.AppendChild(icon);

        var header = new FrameworkElementFactory(typeof(ContentPresenter), "HeaderHost");
        header.SetValue(Grid.ColumnProperty, 1);
        header.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        header.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        header.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        grid.AppendChild(header);

        if (isSubmenuHeader)
        {
            var arrowBrush = foreground ?? SystemColors.MenuTextBrush;
            var arrow = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path), "Arrow");
            arrow.SetValue(Grid.ColumnProperty, 3);
            arrow.SetValue(System.Windows.Shapes.Path.DataProperty,
                Geometry.Parse("M 0 0 L 0 7 L 4 3.5 Z"));
            arrow.SetValue(System.Windows.Shapes.Shape.FillProperty, arrowBrush);
            arrow.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 0, 0, 0));
            grid.AppendChild(arrow);

            var popup = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.Popup), "PART_Popup");
            popup.SetValue(System.Windows.Controls.Primitives.Popup.PlacementProperty,
                System.Windows.Controls.Primitives.PlacementMode.Right);
            popup.SetValue(System.Windows.Controls.Primitives.Popup.HorizontalOffsetProperty, -2.0);
            popup.SetBinding(System.Windows.Controls.Primitives.Popup.IsOpenProperty,
                new System.Windows.Data.Binding("IsSubmenuOpen")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                        System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
            popup.SetValue(System.Windows.Controls.Primitives.Popup.AllowsTransparencyProperty, true);
            popup.SetValue(System.Windows.Controls.Primitives.Popup.FocusableProperty, false);
            popup.SetValue(System.Windows.Controls.Primitives.Popup.PopupAnimationProperty,
                System.Windows.Controls.Primitives.PopupAnimation.Fade);

            var popupBorder = new FrameworkElementFactory(typeof(Border), "SubmenuBorder");
            popupBorder.SetValue(Border.BackgroundProperty, background);
            popupBorder.SetValue(Border.BorderBrushProperty,
                new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)));
            popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            popupBorder.SetValue(Border.PaddingProperty, new Thickness(2));
            popupBorder.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

            var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.SetValue(ScrollViewer.CanContentScrollProperty, true);
            scrollViewer.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);

            var itemsPanel = new FrameworkElementFactory(typeof(StackPanel));
            itemsPanel.SetValue(Panel.IsItemsHostProperty, true);
            itemsPanel.SetValue(KeyboardNavigation.DirectionalNavigationProperty,
                KeyboardNavigationMode.Cycle);
            scrollViewer.AppendChild(itemsPanel);
            popupBorder.AppendChild(scrollViewer);
            popup.AppendChild(popupBorder);
            grid.AppendChild(popup);
        }
        else
        {
            var gesture = new FrameworkElementFactory(typeof(TextBlock), "InputGestureText");
            gesture.SetValue(Grid.ColumnProperty, 2);
            gesture.SetBinding(TextBlock.TextProperty,
                new System.Windows.Data.Binding("InputGestureText")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                        System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
            gesture.SetValue(FrameworkElement.MarginProperty, new Thickness(16, 0, 0, 0));
            gesture.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            gesture.SetValue(TextBlock.ForegroundProperty,
                foreground ?? SystemColors.MenuTextBrush);
            grid.AppendChild(gesture);
        }

        bd.AppendChild(grid);
        tmpl.VisualTree = bd;

        // Triggers
        var highlightTrigger = new Trigger
        {
            Property = MenuItem.IsHighlightedProperty,
            Value = true
        };
        highlightTrigger.Setters.Add(new Setter(Border.BackgroundProperty, hoverBrush) { TargetName = "Bd" });
        tmpl.Triggers.Add(highlightTrigger);

        var disabledTrigger = new Trigger
        {
            Property = UIElement.IsEnabledProperty,
            Value = false
        };
        disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.5));
        tmpl.Triggers.Add(disabledTrigger);

        var noIconTrigger = new Trigger
        {
            Property = MenuItem.IconProperty,
            Value = null
        };
        noIconTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed) { TargetName = "Icon" });
        tmpl.Triggers.Add(noIconTrigger);

        return tmpl;
    }
}