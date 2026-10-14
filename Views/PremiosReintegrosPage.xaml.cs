using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class PremiosReintegrosPage : ContentPage
{
    private readonly PremiosReintegrosViewModel _viewModel;

    public PremiosReintegrosPage(PremiosReintegrosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Carga el histórico real del backend (y refresca totales al
        // volver de captura/detalle — patrón Depósitos/Tickets).
        await _viewModel.AlAparecerAsync();
    }
}
