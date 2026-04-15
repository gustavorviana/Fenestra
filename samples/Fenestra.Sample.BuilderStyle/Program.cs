using Fenestra.Sample.BuilderStyle;
using Fenestra.Windows.Extensions;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;

var builder = FenestraApplication.CreateBuilder(args);
builder.UseMainWindow<MainWindow>();
builder.UseErrorHandler();
builder.UseSplashScreen<SampleSplashScreen>();
builder.Services.AddWpfMinimizeToTray();
builder.Services.AddWindowsCredentialVault();
builder.Services.AddWindowsIdleDetection(opts => opts.Threshold = TimeSpan.FromSeconds(10));
builder.Services.AddWindowsAppLifecycle();
builder.Services.AddWindowsJumpList();
builder.Services.AddWindowsTaskbarOverlay();
builder.Services.AddWindowsLocalization(opts =>
{
    opts.Supported = new[] { "en-US", "pt-BR", "es-ES" };
    opts.Default = "en-US";
    opts.AutoDiscoverFrom(typeof(MainWindow).Assembly);
});
builder.RegisterWindows();

var app = builder.Build();
app.Run();
