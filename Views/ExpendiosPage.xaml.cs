using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Expendios (mockups 1-4): tab del Shell con pestaña 1 Consulta de registros
/// y pestaña 2 Administración de expendios. Modal de fechas/expendios en overlay.
/// </summary>
public partial class ExpendiosPage : ContentPage
{
    private readonly ExpendiosViewModel _vm;

    public ExpendiosPage(ExpendiosViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    /// <summary>
    /// Absorbe el tap sobre la card del modal para que no se propague al
    /// overlay de fondo (que sí cierra).
    /// </summary>
    private void CardModal_Tapped(object? sender, TappedEventArgs e)
    {
        // Intencionalmente vacío: solo consume el gesto.
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.AlAparecer();
    }
}
