using MiCachito.Mobile.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MiCachito.Mobile.Navigation;

/// <summary>
/// Reemplaza la página raíz de la ventana actual.
/// Home y Login son raíces distintas (Shell vs ContentPage), por eso no se navega
/// con Shell.GoToAsync sino asignando Window.Page.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public Task NavigateToHomeAsync()
    {
        GetWindow().Page = _services.GetRequiredService<AppShell>();
        return Task.CompletedTask;
    }

    public Task NavigateToLoginAsync()
    {
        GetWindow().Page = _services.GetRequiredService<LoginPage>();
        return Task.CompletedTask;
    }

    private static Window GetWindow()
    {
        var current = Application.Current
            ?? throw new InvalidOperationException("No hay una Application activa.");

        if (current.Windows.Count == 0)
        {
            throw new InvalidOperationException("No hay ventanas activas.");
        }

        return current.Windows[0];
    }
}
