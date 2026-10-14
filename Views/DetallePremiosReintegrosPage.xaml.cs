using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class DetallePremiosReintegrosPage : ContentPage
{
    private readonly DetallePremiosReintegrosViewModel _viewModel;

    public DetallePremiosReintegrosPage(DetallePremiosReintegrosViewModel viewModel)
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
}
