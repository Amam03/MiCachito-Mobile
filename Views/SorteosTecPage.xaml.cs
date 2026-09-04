using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class SorteosTecPage : ContentPage
{
    public SorteosTecPage(SorteosTecViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Al volver de la 13/14 refresca el badge del carrito flotante
    /// (agregar/eliminar billetes pudo cambiarlo).
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as SorteosTecViewModel)?.AlAparecer();
    }
}
