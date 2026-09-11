using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pestaña Cuenta. Code-behind con DI (patrón GestionPage): el VM se
/// inyecta y se recarga la identidad en OnAppearing.
/// </summary>
public partial class CuentaPage : ContentPage
{
    private readonly CuentaViewModel _viewModel;

    public CuentaPage(CuentaViewModel viewModel)
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
    /// scrim y cierre el diálogo (patrón ExpendiosPage).
    /// </summary>
    private void CardModal_Tapped(object? sender, TappedEventArgs e)
    {
        // Intencionalmente vacío: consume el gesto.
    }
}
