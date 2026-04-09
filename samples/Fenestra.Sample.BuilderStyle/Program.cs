using Fenestra.Sample.BuilderStyle;
using Fenestra.Windows;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;

var builder = FenestraApplication.CreateBuilder(args);
builder.UseMainWindow<MainWindow>();
builder.Services.AddWpfMinimizeToTray();
builder.Services.AddWindowsCredentialVault();
builder.RegisterWindows();

var app = builder.Build();
app.Run();
