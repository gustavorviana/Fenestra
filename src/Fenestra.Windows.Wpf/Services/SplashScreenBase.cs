using Fenestra.Core;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Fenestra.Wpf.Services;

/// <summary>
/// Abstract base class for image-style splash screens. Derive from this, configure the
/// customization dependency properties (colors, logo, title) in your constructor, and
/// override <see cref="LoadAsync"/> with the app's loading logic. The base class provides
/// a built-in dark card visual driven by the customization DPs — no XAML is required.
/// </summary>
/// <remarks>
/// <para>
/// The base class drives the full lifecycle:
/// <list type="number">
///   <item><description><see cref="Window.Show"/> is invoked before loading starts.</description></item>
///   <item><description><see cref="LoadAsync"/> runs with a progress reporter tied to both
///     <see cref="OnProgressChanged"/> (which by default updates <see cref="StatusMessage"/>,
///     <see cref="ProgressValue"/> and <see cref="IsIndeterminate"/>) and the external observer
///     supplied by the Fenestra pipeline.</description></item>
///   <item><description><see cref="Window.Close"/> is invoked in a <c>finally</c> block so
///     the splash is always torn down — even when loading throws.</description></item>
/// </list>
/// </para>
/// <para>
/// Two customization paths are supported:
/// <list type="bullet">
///   <item><description><b>Default visual (no XAML):</b> set the customization DPs
///     (<see cref="CardBackground"/>, <see cref="AccentBrush"/>, <see cref="LogoSource"/>,
///     <see cref="AppTitle"/>, etc.) in the derived constructor.</description></item>
///   <item><description><b>Custom XAML:</b> declare the derived class with an
///     <c>x:Class</c> XAML root rooted at <c>services:ImageSplashScreenBase</c>. The XAML's
///     <c>Content</c> overrides the built-in card. You may bind your custom controls to
///     <see cref="StatusMessage"/> / <see cref="ProgressValue"/> / <see cref="IsIndeterminate"/>
///     to keep using the default <see cref="OnProgressChanged"/>, or override
///     <see cref="OnProgressChanged"/> to update named XAML elements directly.</description></item>
/// </list>
/// </para>
/// </remarks>
public abstract class SplashScreenBase : Window, ISplashScreen
{
    // ============================================================================
    // Customization DPs — set these in the derived constructor.
    // ============================================================================

    /// <summary>Background brush of the rounded card. Default: dark slate (#1F2430).</summary>
    public static readonly DependencyProperty CardBackgroundProperty = DependencyProperty.Register(
        nameof(CardBackground), typeof(Brush), typeof(SplashScreenBase),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x1F, 0x24, 0x30))));

    /// <inheritdoc cref="CardBackgroundProperty" />
    public Brush CardBackground
    {
        get => (Brush)GetValue(CardBackgroundProperty);
        set => SetValue(CardBackgroundProperty, value);
    }

    /// <summary>Border brush of the rounded card. Default: muted blue-gray (#3A4256).</summary>
    public static readonly DependencyProperty CardBorderBrushProperty = DependencyProperty.Register(
        nameof(CardBorderBrush), typeof(Brush), typeof(SplashScreenBase),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x3A, 0x42, 0x56))));

    /// <inheritdoc cref="CardBorderBrushProperty" />
    public Brush CardBorderBrush
    {
        get => (Brush)GetValue(CardBorderBrushProperty);
        set => SetValue(CardBorderBrushProperty, value);
    }

    /// <summary>Accent brush — drives the progress bar fill and the logo fallback ellipse. Default: blue (#5B8FF9).</summary>
    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush), typeof(Brush), typeof(SplashScreenBase),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x5B, 0x8F, 0xF9))));

    /// <inheritdoc cref="AccentBrushProperty" />
    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    /// <summary>Foreground brush of the title text. Default: white.</summary>
    public static readonly DependencyProperty TitleForegroundProperty = DependencyProperty.Register(
        nameof(TitleForeground), typeof(Brush), typeof(SplashScreenBase),
        new PropertyMetadata(Brushes.White));

    /// <inheritdoc cref="TitleForegroundProperty" />
    public Brush TitleForeground
    {
        get => (Brush)GetValue(TitleForegroundProperty);
        set => SetValue(TitleForegroundProperty, value);
    }

    /// <summary>Foreground brush of the subtitle text. Default: light gray (#98A1B3).</summary>
    public static readonly DependencyProperty SubtitleForegroundProperty = DependencyProperty.Register(
        nameof(SubtitleForeground), typeof(Brush), typeof(SplashScreenBase),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x98, 0xA1, 0xB3))));

    /// <inheritdoc cref="SubtitleForegroundProperty" />
    public Brush SubtitleForeground
    {
        get => (Brush)GetValue(SubtitleForegroundProperty);
        set => SetValue(SubtitleForegroundProperty, value);
    }

    /// <summary>Foreground brush of the status message. Default: off-white (#D7DCEA).</summary>
    public static readonly DependencyProperty StatusForegroundProperty = DependencyProperty.Register(
        nameof(StatusForeground), typeof(Brush), typeof(SplashScreenBase),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xD7, 0xDC, 0xEA))));

    /// <inheritdoc cref="StatusForegroundProperty" />
    public Brush StatusForeground
    {
        get => (Brush)GetValue(StatusForegroundProperty);
        set => SetValue(StatusForegroundProperty, value);
    }

    /// <summary>Track brush of the progress bar. Default: dark gray-blue (#2B3142).</summary>
    public static readonly DependencyProperty ProgressTrackBrushProperty = DependencyProperty.Register(
        nameof(ProgressTrackBrush), typeof(Brush), typeof(SplashScreenBase),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x2B, 0x31, 0x42))));

    /// <inheritdoc cref="ProgressTrackBrushProperty" />
    public Brush ProgressTrackBrush
    {
        get => (Brush)GetValue(ProgressTrackBrushProperty);
        set => SetValue(ProgressTrackBrushProperty, value);
    }

    /// <summary>
    /// Logo image displayed in the header. When <c>null</c> (default), an
    /// <see cref="Ellipse"/> filled with <see cref="AccentBrush"/> is shown as a placeholder.
    /// </summary>
    public static readonly DependencyProperty LogoSourceProperty = DependencyProperty.Register(
        nameof(LogoSource), typeof(ImageSource), typeof(SplashScreenBase),
        new PropertyMetadata(null));

    /// <inheritdoc cref="LogoSourceProperty" />
    public ImageSource? LogoSource
    {
        get => (ImageSource?)GetValue(LogoSourceProperty);
        set => SetValue(LogoSourceProperty, value);
    }

    /// <summary>App title displayed next to the logo. Default: empty string.</summary>
    public static readonly DependencyProperty AppTitleProperty = DependencyProperty.Register(
        nameof(AppTitle), typeof(string), typeof(SplashScreenBase),
        new PropertyMetadata(string.Empty));

    /// <inheritdoc cref="AppTitleProperty" />
    public string AppTitle
    {
        get => (string)GetValue(AppTitleProperty);
        set => SetValue(AppTitleProperty, value);
    }

    /// <summary>App subtitle displayed below the title. Default: empty string.</summary>
    public static readonly DependencyProperty AppSubtitleProperty = DependencyProperty.Register(
        nameof(AppSubtitle), typeof(string), typeof(SplashScreenBase),
        new PropertyMetadata(string.Empty));

    /// <inheritdoc cref="AppSubtitleProperty" />
    public string AppSubtitle
    {
        get => (string)GetValue(AppSubtitleProperty);
        set => SetValue(AppSubtitleProperty, value);
    }

    /// <summary>
    /// Optional background photo painted inside the rounded card behind the content.
    /// When <c>null</c> (default), only <see cref="CardBackground"/> is shown. The image is
    /// stretched with <see cref="Stretch.UniformToFill"/> so it covers the card without
    /// distortion (cropping if necessary), and is clipped to the card's rounded corners.
    /// </summary>
    public static readonly DependencyProperty BackgroundImageSourceProperty = DependencyProperty.Register(
        nameof(BackgroundImageSource), typeof(ImageSource), typeof(SplashScreenBase),
        new PropertyMetadata(null));

    /// <inheritdoc cref="BackgroundImageSourceProperty" />
    public ImageSource? BackgroundImageSource
    {
        get => (ImageSource?)GetValue(BackgroundImageSourceProperty);
        set => SetValue(BackgroundImageSourceProperty, value);
    }

    /// <summary>Width of the rounded card (and therefore the splash window). Default: 480.</summary>
    public static readonly DependencyProperty CardWidthProperty = DependencyProperty.Register(
        nameof(CardWidth), typeof(double), typeof(SplashScreenBase),
        new PropertyMetadata(480.0));

    /// <inheritdoc cref="CardWidthProperty" />
    public double CardWidth
    {
        get => (double)GetValue(CardWidthProperty);
        set => SetValue(CardWidthProperty, value);
    }

    /// <summary>Height of the rounded card (and therefore the splash window). Default: 260.</summary>
    public static readonly DependencyProperty CardHeightProperty = DependencyProperty.Register(
        nameof(CardHeight), typeof(double), typeof(SplashScreenBase),
        new PropertyMetadata(260.0));

    /// <inheritdoc cref="CardHeightProperty" />
    public double CardHeight
    {
        get => (double)GetValue(CardHeightProperty);
        set => SetValue(CardHeightProperty, value);
    }

    /// <summary>
    /// When <c>true</c>, shows a close ("×") button in the top-right corner of the card.
    /// Clicking it cancels the loading logic via the <see cref="CancellationToken"/> passed
    /// to <see cref="LoadAsync"/> and causes the Fenestra pipeline to shut the application
    /// down instead of showing the main window. Default: <c>false</c>.
    /// </summary>
    public static readonly DependencyProperty ShowCloseButtonProperty = DependencyProperty.Register(
        nameof(ShowCloseButton), typeof(bool), typeof(SplashScreenBase),
        new PropertyMetadata(false));

    /// <inheritdoc cref="ShowCloseButtonProperty" />
    public bool ShowCloseButton
    {
        get => (bool)GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    // ============================================================================
    // State DPs — updated by the default OnProgressChanged. Bind to these from
    // custom XAML to keep using the base progress wiring.
    // ============================================================================

    /// <summary>Latest status message reported by <see cref="LoadAsync"/>.</summary>
    public static readonly DependencyProperty StatusMessageProperty = DependencyProperty.Register(
        nameof(StatusMessage), typeof(string), typeof(SplashScreenBase),
        new PropertyMetadata(string.Empty));

    /// <inheritdoc cref="StatusMessageProperty" />
    public string StatusMessage
    {
        get => (string)GetValue(StatusMessageProperty);
        set => SetValue(StatusMessageProperty, value);
    }

    /// <summary>Latest determinate progress value in the range [0, 1].</summary>
    public static readonly DependencyProperty ProgressValueProperty = DependencyProperty.Register(
        nameof(ProgressValue), typeof(double), typeof(SplashScreenBase),
        new PropertyMetadata(0.0));

    /// <inheritdoc cref="ProgressValueProperty" />
    public double ProgressValue
    {
        get => (double)GetValue(ProgressValueProperty);
        set => SetValue(ProgressValueProperty, value);
    }

    /// <summary>Whether the progress bar should display in indeterminate mode.</summary>
    public static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register(
        nameof(IsIndeterminate), typeof(bool), typeof(SplashScreenBase),
        new PropertyMetadata(false));

    /// <inheritdoc cref="IsIndeterminateProperty" />
    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>
    /// Initializes a splash window with sensible defaults for a borderless, transparent,
    /// top-most surface and assigns the built-in card visual as <see cref="ContentControl.Content"/>.
    /// Derived classes that supply their own XAML <c>Content</c> automatically override this
    /// (XAML loading runs after the base constructor).
    /// </summary>
    protected SplashScreenBase()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        SizeToContent = SizeToContent.WidthAndHeight;

        // Click-anywhere drag. With AllowsTransparency=true the transparent margin around the
        // card is hit-test invisible, so this only fires for clicks landing on the card itself.
        MouseLeftButtonDown += OnSplashMouseLeftButtonDown;

        Content = BuildDefaultContent();
    }

    private void OnSplashMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // DragMove() must be invoked from a left-button-down handler and would throw if the
        // button was already released by the time we get here.
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    private FrameworkElement BuildDefaultContent()
    {
        // Rounded card with drop shadow against the transparent window surface.
        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 0,
                Opacity = 0.5,
                Color = Colors.Black,
            },
        };
        Bind(card, FrameworkElement.WidthProperty, nameof(CardWidth));
        Bind(card, FrameworkElement.HeightProperty, nameof(CardHeight));
        Bind(card, Border.BackgroundProperty, nameof(CardBackground));
        Bind(card, Border.BorderBrushProperty, nameof(CardBorderBrush));

        // Layered root: a Grid that holds (1) an optional background image clipped to the
        // card's rounded corners, and (2) the actual content on top.
        var root = new Grid();

        // Background image layer. Uses CornerRadius=11 to fit inside the outer 12-radius
        // border with its 1-pixel thickness — keeps the rounded corners flush.
        var backgroundImageBorder = new Border { CornerRadius = new CornerRadius(11) };
        UpdateBackgroundImageBrush(backgroundImageBorder);
        DependencyPropertyDescriptor.FromProperty(BackgroundImageSourceProperty, typeof(SplashScreenBase))
            .AddValueChanged(this, (_, _) => UpdateBackgroundImageBrush(backgroundImageBorder));
        root.Children.Add(backgroundImageBorder);

        var grid = new Grid { Margin = new Thickness(32, 28, 32, 24) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // ── Header: logo + title/subtitle ────────────────────────────────────
        var header = new StackPanel { Orientation = Orientation.Horizontal };
        Grid.SetRow(header, 0);

        // Logo container holds both an Image (for LogoSource) and an Ellipse fallback;
        // visibility flips based on whether LogoSource is set. Tracking via DependencyPropertyDescriptor
        // means runtime changes to LogoSource are reflected immediately.
        var logoContainer = new Grid { Width = 36, Height = 36 };
        var logoFallback = new Ellipse();
        Bind(logoFallback, Shape.FillProperty, nameof(AccentBrush));
        var logoImage = new Image { Stretch = Stretch.Uniform };
        Bind(logoImage, Image.SourceProperty, nameof(LogoSource));
        logoContainer.Children.Add(logoFallback);
        logoContainer.Children.Add(logoImage);
        UpdateLogoVisibility(logoImage, logoFallback);
        DependencyPropertyDescriptor.FromProperty(LogoSourceProperty, typeof(SplashScreenBase))
            .AddValueChanged(this, (_, _) => UpdateLogoVisibility(logoImage, logoFallback));
        header.Children.Add(logoContainer);

        var titleStack = new StackPanel
        {
            Margin = new Thickness(14, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var titleText = new TextBlock { FontSize = 18, FontWeight = FontWeights.SemiBold };
        Bind(titleText, TextBlock.TextProperty, nameof(AppTitle));
        Bind(titleText, TextBlock.ForegroundProperty, nameof(TitleForeground));
        var subtitleText = new TextBlock { FontSize = 11 };
        Bind(subtitleText, TextBlock.TextProperty, nameof(AppSubtitle));
        Bind(subtitleText, TextBlock.ForegroundProperty, nameof(SubtitleForeground));
        titleStack.Children.Add(titleText);
        titleStack.Children.Add(subtitleText);
        header.Children.Add(titleStack);

        grid.Children.Add(header);

        // ── Status text ──────────────────────────────────────────────────────
        var status = new TextBlock { FontSize = 13, Margin = new Thickness(0, 0, 0, 10) };
        Grid.SetRow(status, 2);
        Bind(status, TextBlock.TextProperty, nameof(StatusMessage));
        Bind(status, TextBlock.ForegroundProperty, nameof(StatusForeground));
        grid.Children.Add(status);

        // ── Progress bar ─────────────────────────────────────────────────────
        var progressBar = new ProgressBar
        {
            Height = 4,
            Minimum = 0,
            Maximum = 1,
            BorderThickness = new Thickness(0),
        };
        Grid.SetRow(progressBar, 3);
        Bind(progressBar, RangeBase.ValueProperty, nameof(ProgressValue));
        Bind(progressBar, ProgressBar.IsIndeterminateProperty, nameof(IsIndeterminate));
        Bind(progressBar, ForegroundProperty, nameof(AccentBrush));
        Bind(progressBar, BackgroundProperty, nameof(ProgressTrackBrush));
        grid.Children.Add(progressBar);

        root.Children.Add(grid);

        // ── Close button ("×") in the top-right corner ────────────────────────
        // Built as a Border + TextBlock (rather than a Button) so the visuals match the
        // card's theme without needing a ControlTemplate. MouseLeftButtonDown is marked
        // handled so it doesn't bubble up to the window's DragMove handler.
        var closeButton = new Border
        {
            Width = 28,
            Height = 28,
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 8, 8, 0),
            Cursor = System.Windows.Input.Cursors.Hand,
        };
        var closeGlyph = new TextBlock
        {
            Text = "\u00D7", // ×
            FontSize = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Bind(closeGlyph, TextBlock.ForegroundProperty, nameof(SubtitleForeground));
        closeButton.Child = closeGlyph;

        BindingOperations.SetBinding(
            closeButton,
            UIElement.VisibilityProperty,
            new Binding(nameof(ShowCloseButton))
            {
                Source = this,
                Converter = new BooleanToVisibilityConverter(),
            });

        closeButton.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true; // prevent the window's DragMove handler from running
            Close();
        };
        root.Children.Add(closeButton);

        card.Child = root;
        return card;
    }

    private void Bind(DependencyObject target, DependencyProperty property, string sourcePath)
        => BindingOperations.SetBinding(target, property, new Binding(sourcePath) { Source = this });

    private static void UpdateLogoVisibility(Image image, Ellipse fallback)
    {
        var hasSource = image.Source != null;
        image.Visibility = hasSource ? Visibility.Visible : Visibility.Collapsed;
        fallback.Visibility = hasSource ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateBackgroundImageBrush(Border target)
    {
        var source = BackgroundImageSource;
        if (source != null)
        {
            target.Background = new ImageBrush(source) { Stretch = Stretch.UniformToFill };
            target.Visibility = Visibility.Visible;
        }
        else
        {
            target.Background = null;
            target.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Runs the application's loading logic. Override this with whatever initialization
    /// must complete before the main window may be shown — loading settings, opening a
    /// database connection, authenticating a user, pre-warming caches, etc. Report status
    /// updates via <paramref name="progress"/> so the splash UI (and any external observers)
    /// can reflect the current step.
    /// </summary>
    /// <param name="progress">
    /// Progress reporter tied to the UI thread — calls to <see cref="IProgress{T}.Report"/>
    /// are marshaled onto the dispatcher and then forwarded to <see cref="OnProgressChanged"/>.
    /// </param>
    /// <param name="cancellationToken">Token signaled when the app is shutting down.</param>
    protected abstract Task LoadAsync(IProgress<SplashStatus> progress, CancellationToken cancellationToken);

    /// <summary>
    /// Called on the UI thread whenever <see cref="LoadAsync"/> reports progress. The default
    /// implementation updates <see cref="StatusMessage"/>, <see cref="ProgressValue"/> and
    /// <see cref="IsIndeterminate"/>, which the built-in card visual is bound to. Override
    /// (and optionally call <c>base.OnProgressChanged(status)</c>) when you need to update
    /// custom XAML elements directly.
    /// </summary>
    protected virtual void OnProgressChanged(SplashStatus status)
    {
        StatusMessage = status.Message;
        if (status.Percent is { } percent)
        {
            IsIndeterminate = false;
            ProgressValue = percent;
        }
        else
        {
            IsIndeterminate = true;
        }
    }

    /// <inheritdoc />
    async Task ISplashScreen.RunAsync(IProgress<SplashStatus> progress, CancellationToken cancellationToken)
    {
        // Linked CTS so the user clicking the close button (or closing the window some other
        // way) can cancel LoadAsync in addition to any external shutdown signal. We only own
        // the local CTS; disposing it does not affect the external token.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var userClosed = false;
        var loadingCompleted = false;

        void OnSplashClosing(object? sender, CancelEventArgs e)
        {
            // Ignore the Close() call that the finally block issues after a successful load.
            if (loadingCompleted) return;
            userClosed = true;
            linkedCts.Cancel();
        }

        Closing += OnSplashClosing;

        Show();

        // Give the dispatcher one tick so the splash actually renders before LoadAsync takes over.
        // Without this, a synchronous LoadAsync would complete before any paint message is pumped
        // and the user would never see the splash.
        await Task.Yield();

        // Fan-out progress: update our own UI *and* forward to the pipeline's observer so
        // external subscribers (logging, telemetry, tests) see the same stream the user sees.
        // Progress<T> captures the current SynchronizationContext, so the handler runs on the
        // UI thread as long as this method was entered on the dispatcher.
        var composite = new Progress<SplashStatus>(status =>
        {
            OnProgressChanged(status);
            progress?.Report(status);
        });

        try
        {
            await LoadAsync(composite, linkedCts.Token);
            loadingCompleted = true;
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            // Swallowed here; we rethrow below with the correct "cancelled" semantics so
            // the pipeline can distinguish a user-cancelled splash from a normal completion.
        }
        finally
        {
            Closing -= OnSplashClosing;
            if (!userClosed)
            {
                Close();
            }
        }

        if (userClosed || cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                userClosed
                    ? "Splash screen was closed by the user."
                    : "Splash screen was cancelled before loading completed.",
                cancellationToken);
        }
    }
}
