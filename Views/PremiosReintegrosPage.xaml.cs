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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Refresca totales agregados al volver de captura/detalle (patron
        // del proyecto: las propiedades derivadas no se recalculan solas).
        _viewModel.AlAparecer();
    }
}
