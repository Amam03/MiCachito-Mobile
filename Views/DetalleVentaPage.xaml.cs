using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Detalle de Venta" (mockup 5.1): datos de la venta REAL (GET
/// api/mobile/ventas/{id}) según tipo + acciones Imprimir (Bluetooth) y
/// Compartir (PDF).
/// </summary>
public partial class DetalleVentaPage : ContentPage
{
    private readonly DetalleVentaViewModel _vm;

    public DetalleVentaPage(DetalleVentaViewModel vm)
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
