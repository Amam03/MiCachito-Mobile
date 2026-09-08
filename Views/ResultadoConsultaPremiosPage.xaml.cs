using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class ResultadoConsultaPremiosPage : ContentPage
{
    private readonly ResultadoConsultaPremiosViewModel _viewModel;

    public ResultadoConsultaPremiosPage(ResultadoConsultaPremiosViewModel viewModel)
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
