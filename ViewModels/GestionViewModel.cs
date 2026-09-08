using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pestaña Gestion (mockup 1: pantalla principal con saldo
/// y menu de 9 modulos). Fase SOLO INTERFAZ: el saldo es el valor verbatim
/// del mockup y los comandos de modulo quedan preparados como stubs para
/// conectar las pantallas correspondientes despues.
/// </summary>
public partial class GestionViewModel : BaseViewModel
{
    private string _saldo = "$0.00";

    /// <summary>
    /// Saldo mostrado en el encabezado (mockup: "$0.00" en verde brillante).
    /// </summary>
    public string Saldo
    {
        get => _saldo;
        set => SetProperty(ref _saldo, value);
    }

    public GestionViewModel()
    {
        Title = "Gestion";
    }

    /// <summary>
    /// Abre un modulo del menu. Los modulos con pantalla implementada navegan
    /// a ella; el resto queda como stub de solo-interfaz.
    /// Parametros (CommandParameter de cada tarjeta):
    ///   consultaPremios  = Consulta de Premios   (mockups 2, 2.1)
    ///   devolucion       = Devolucion            (mockups 6, 6.1, 6.2)
    ///   premiosReintegros = Premios y Reintegros (mockups 3, 3.1, 3.2)
    ///   depositos        = Depositos             (mockups 7, 7.1, 7.2, 7.3)
    ///   sorteos          = Sorteos               (mockups 4, 4.1, 4.2)
    ///   recibosPago      = Recibos de Pago       (mockups 8, 8.1, 8.2, 8.3)
    ///   ticketsVenta     = Tickets de Venta      (mockups 5, 5.1, 5.2)
    ///   reportes         = Reportes
    ///   tiraLiquidacion  = Tira de Liquidacion
    /// </summary>
    [RelayCommand]
    private Task AbrirModuloAsync(string modulo)
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return modulo switch
        {
            "consultaPremios" => Shell.Current.GoToAsync(nameof(Views.ConsultaPremiosPage)),
            "premiosReintegros" => Shell.Current.GoToAsync(nameof(Views.PremiosReintegrosPage)),
            _ => Task.CompletedTask,
        };
    }
}
