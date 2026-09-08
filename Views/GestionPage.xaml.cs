using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class GestionPage : ContentPage
{
    private readonly GestionViewModel _viewModel;

    public GestionPage(GestionViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }
}
