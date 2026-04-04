using Fenestra.Sample.BuilderStyle;
using Fenestra.Wpf;

var builder = FenestraApplication.CreateBuilder(args);
builder.UseMainWindow<MainWindow>();
builder.UseMinimizeToTray();
builder.RegisterWindows();

var app = builder.Build();
app.Run();
