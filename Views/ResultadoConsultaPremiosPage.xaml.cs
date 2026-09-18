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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.AlAparecerAsync();
        }
        catch
        {
            // El VM ya maneja sus propios estados de error; esto solo evita
            // que una excepción no controlada rompa la navegación.
        }
    }
}
