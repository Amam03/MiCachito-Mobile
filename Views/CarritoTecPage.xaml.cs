using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class CarritoTecPage : ContentPage
{
    public CarritoTecPage(CarritoTecViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Al volver de la 15 (Confirmar Venta vació el carrito):
    /// reconstruye los registros y refresca totales/badge.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as CarritoTecViewModel)?.AlAparecer();
    }
}
