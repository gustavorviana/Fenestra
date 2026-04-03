using Fenestra.Wpf;
using Fenestra.Sample.BuilderWithApp;

var builder = FenestraApplication.CreateBuilder<App, MainWindow>(args);
builder.RegisterWindows();

var app = builder.Build();
app.Run();
