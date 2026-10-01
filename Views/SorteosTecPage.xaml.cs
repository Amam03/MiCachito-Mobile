using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class SorteosTecPage : ContentPage
{
    private readonly SorteosTecViewModel _viewModel;

    public SorteosTecPage(SorteosTecViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    /// <summary>
    /// Al aparecer, carga los sorteos TEC del billetero desde la API.
    /// Antes la lista venía de SorteosTecData (catálogo fijo con ids
    /// desfasados y billetes inventados que no existían en la BD).
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CargarAsync();
    }
}
