using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Detalle de Venta" (mockup 5.1): datos del movimiento
/// según tipo + acciones Imprimir (Bluetooth) y Compartir (PDF).
/// Fase SOLO INTERFAZ.
/// </summary>
public partial class DetalleVentaPage : ContentPage
{
    private readonly DetalleVentaViewModel _vm;

    public DetalleVentaPage(DetalleVentaViewModel vm)
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
