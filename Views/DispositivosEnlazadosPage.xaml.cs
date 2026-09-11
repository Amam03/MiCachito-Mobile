using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Dispositivos Enlazados (mockup 5). Recarga los dispositivos
/// vinculados al aparecer (patrón AlAparecer).
/// </summary>
public partial class DispositivosEnlazadosPage : ContentPage
{
    private readonly DispositivosEnlazadosViewModel _viewModel;

    public DispositivosEnlazadosPage(DispositivosEnlazadosViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel is not null)
        {
            await _viewModel.AlAparecerAsync();
        }
    }
}
