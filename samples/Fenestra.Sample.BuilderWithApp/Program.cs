using Fenestra.Sample.BuilderWithApp;
using Fenestra.Wpf;

var builder = FenestraApplication.CreateBuilder<App, MainWindow>(args);
builder.RegisterWindows();

var app = builder.Build();
app.Run();
