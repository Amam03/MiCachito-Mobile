using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Escanear Series" (mockup 6.2 escaneo): mismo componente visual
/// de escaneo que Premios y Reintegros (CameraScannerView + marco +
/// linterna) con entrada manual como vía de prueba en emulador.
/// Fase solo-interfaz.
/// </summary>
public partial class EscanearSeriesPage : ContentPage
{
    private readonly EscanearSeriesViewModel _vm;

    public EscanearSeriesPage(EscanearSeriesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.AlAparecerAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.AlDesaparecer();
    }
}
