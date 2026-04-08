using Fenestra.Sample.BuilderStyle;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;

var builder = FenestraApplication.CreateBuilder(args);
builder.UseMainWindow<MainWindow>();
builder.Services.AddWpfMinimizeToTray();
builder.RegisterWindows();

var app = builder.Build();
app.Run();
