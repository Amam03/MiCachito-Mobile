using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Detalle de Pago" (mockups 8.1/8.2): header azul con Folio,
/// Fecha y Total, desglose de la operación, FAB de descarga con overlay
/// y aviso verde. Conectada a api/mobile/pagos/recibo/{id}.
/// </summary>
public partial class DetallePagoPage : ContentPage
{
    private readonly DetallePagoViewModel _viewModel;

    public DetallePagoPage(DetallePagoViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
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
