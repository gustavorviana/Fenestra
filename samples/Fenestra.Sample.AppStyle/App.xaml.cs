using System.Windows;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace Fenestra.Sample.AppStyle;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseMinimizeToTray();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}
