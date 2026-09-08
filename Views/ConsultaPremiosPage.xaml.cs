using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class ConsultaPremiosPage : ContentPage
{
    private readonly ConsultaPremiosViewModel _viewModel;

    public ConsultaPremiosPage(ConsultaPremiosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.AlAparecerAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Oculta el teclado del telefono al salir de la pantalla (p. ej. al
        // navegar al resultado tras una consulta exitosa).
        CampoManual.Unfocus();
        _viewModel.AlDesaparecer();
    }
}
