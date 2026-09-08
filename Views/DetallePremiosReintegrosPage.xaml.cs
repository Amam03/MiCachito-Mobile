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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.AlAparecer();
    }
}
