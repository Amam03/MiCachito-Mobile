using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla Sorteos Celebrados (mockups 4, 4.1, 4.2 de Gestión):
/// selector fijado arriba + resumen + secciones colapsables.
/// </summary>
public partial class SorteosPage : ContentPage
{
    private readonly SorteosViewModel _viewModel;

    public SorteosPage(SorteosViewModel viewModel)
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
