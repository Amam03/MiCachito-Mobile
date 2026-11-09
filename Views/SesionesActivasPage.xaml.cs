using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Sesiones Activas (M-06). Recarga las sesiones activas del expendio al
/// aparecer (patrón AlAparecer de DispositivosEnlazados/PremiosReintegros).
/// </summary>
public partial class SesionesActivasPage : ContentPage
{
    private readonly SesionesActivasViewModel _viewModel;

    public SesionesActivasPage(SesionesActivasViewModel viewModel)
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

    /// <summary>
    /// Tap dentro de la card de un modal: evita que el toque llegue al
    /// scrim y cierre el diálogo (patrón CuentaPage/ExpendiosPage).
    /// </summary>
    private void CardModal_Tapped(object? sender, TappedEventArgs e)
    {
        // Intencionalmente vacío: consume el gesto.
    }
}
