using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class AgregarBoletosPage : ContentPage
{
    public AgregarBoletosPage(AgregarBoletosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Al volver de la pantalla del carrito (pantalla 11) refresca los
    /// valores derivados (badge, disponibles): Eliminar/Vender pudieron
    /// cambiar las selecciones de las tiendas.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as AgregarBoletosViewModel)?.AlAparecer();
    }
}
