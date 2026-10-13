using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Ventas" (mockup 5 de Gestión): listado de ventas REALES del
/// billetero de la sesión (GET api/mobile/ventas) con filtro por tipo.
/// </summary>
public partial class TicketsVentaPage : ContentPage
{
    private readonly TicketsVentaViewModel _vm;

    public TicketsVentaPage(TicketsVentaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.AlAparecerAsync();
    }
}
