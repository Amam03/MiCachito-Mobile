using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Ventas" (mockup 5 de Gestión): listado de ventas de Vender
/// (LOTENAL, Sorteos Tec, Tiempo Aire) con filtro por tipo. Fase SOLO
/// INTERFAZ — movimientos temporales en memoria.
/// </summary>
public partial class TicketsVentaPage : ContentPage
{
    private readonly TicketsVentaViewModel _vm;

    public TicketsVentaPage(TicketsVentaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.AlAparecer();
    }
}
