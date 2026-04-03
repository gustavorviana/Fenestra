using Fenestra.Wpf;
using Fenestra.Sample.BuilderStyle;

var builder = FenestraApplication.CreateBuilder(args);
builder.UseMainWindow<MainWindow>();
builder.UseMinimizeToTray();
builder.RegisterWindows();

var app = builder.Build();
app.Run();
