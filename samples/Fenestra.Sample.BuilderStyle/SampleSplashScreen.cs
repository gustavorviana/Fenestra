using Fenestra.Core;
using Fenestra.Wpf.Services;
using System.Windows.Media;

namespace Fenestra.Sample.BuilderStyle;

/// <summary>
/// Example splash screen derived from <see cref="SplashScreenBase"/>. The base class
/// provides the entire visual (rounded card with logo, title, status text and progress bar)
/// driven by dependency properties — this sample only customizes the colors, sets a logo,
/// and supplies the loading logic in <see cref="LoadAsync"/>. No XAML is needed.
/// </summary>
public class SampleSplashScreen : SplashScreenBase
{
    public SampleSplashScreen()
    {
        AppTitle = "Fenestra Sample";
        AppSubtitle = "Builder Style";
        ShowCloseButton = true;

        // Custom colors. Anything Brush-typed works — gradient brushes, image brushes, etc.
        CardBackground = new SolidColorBrush(Color.FromRgb(0x14, 0x1A, 0x2E));
        CardBorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x3A, 0x5C));
        AccentBrush = new SolidColorBrush(Color.FromRgb(0x7C, 0xC4, 0xFF));

        // Logo: a vector "F" rendered as a DrawingImage so the sample doesn't need a binary
        // asset committed to the repo. In a real app you'd typically use a BitmapImage from
        // a packed resource, e.g.:
        //
        //   LogoSource = new BitmapImage(new Uri(
        //       "pack://application:,,,/Assets/logo.png", UriKind.Absolute));
        //
        LogoSource = BuildLogo();
    }

    private static ImageSource BuildLogo()
    {
        // Stylized "F" — outer dimensions 36x36 to match the slot the base class allocates.
        var geometry = Geometry.Parse(
            "M 6,4 L 30,4 L 30,11 L 14,11 L 14,16 L 27,16 L 27,23 L 14,23 L 14,32 L 6,32 Z");

        var drawing = new GeometryDrawing
        {
            Brush = new SolidColorBrush(Color.FromRgb(0x7C, 0xC4, 0xFF)),
            Geometry = geometry,
        };

        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    /// <summary>
    /// Simulates app-level initialization. In a real app this is where you'd load settings,
    /// open a database, authenticate the user, pre-warm caches, etc. Services are available
    /// via constructor injection — this sample doesn't need any.
    /// </summary>
    protected override async Task LoadAsync(IProgress<SplashStatus> progress, CancellationToken cancellationToken)
    {
        var steps = new (string Message, int DelayMs)[]
        {
            ("Loading configuration...", 1000),
            ("Connecting to services...", 2000),
            ("Preparing workspace...",    1000),
            ("Almost there...",           2000),
        };

        for (var i = 0; i < steps.Length; i++)
        {
            var (message, delay) = steps[i];
            // Percent goes from 0 → 1 across the steps. We report BEFORE the delay so the
            // UI reflects the upcoming step while it's running.
            var percent = (double)(i + 1) / steps.Length;
            progress.Report(new SplashStatus(message, percent));

            await Task.Delay(delay, cancellationToken);
        }
    }
}
