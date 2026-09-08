using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class EscanearBoletosPage : ContentPage
{
    private readonly EscanearBoletosViewModel _viewModel;

    public EscanearBoletosPage(EscanearBoletosViewModel viewModel)
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
        CampoManual.Unfocus();
        _viewModel.AlDesaparecer();
    }
}
