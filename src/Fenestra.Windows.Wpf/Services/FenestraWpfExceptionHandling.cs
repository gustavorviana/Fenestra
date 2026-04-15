using Fenestra.Core;
using Fenestra.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Threading;

namespace Fenestra.Wpf.Services;

/// <summary>
/// Shared WPF exception handler wiring used by both <see cref="FenestraApp"/> (App-style)
/// and <see cref="FenestraApplication"/> (Builder-style). Binds the three standard
/// exception sources (Dispatcher, AppDomain, unobserved Task) to the Fenestra
/// <see cref="IExceptionHandler"/> resolved from DI.
/// </summary>
internal sealed class FenestraWpfExceptionHandling
{
    private readonly IServiceProvider _services;

    public FenestraWpfExceptionHandling(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// Subscribes to WPF dispatcher, AppDomain and unobserved Task exceptions on the
    /// provided <see cref="Application"/>. Handlers live for the lifetime of the app.
    /// </summary>
    public void Attach(Application application)
    {
        application.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var context = new FenestraExceptionContext(e.Exception, isCritical: false);
        HandleException(context);
        e.Handled = context.Handled;
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            var context = new FenestraExceptionContext(ex, isCritical: true);
            HandleException(context);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var context = new FenestraExceptionContext(e.Exception, isCritical: false);
        HandleException(context);
        if (context.Handled)
            e.SetObserved();
    }

    private void HandleException(FenestraExceptionContext context)
    {
        var handler = _services.GetRequiredService<IExceptionHandler>();
        handler.Handle(context);
    }
}
