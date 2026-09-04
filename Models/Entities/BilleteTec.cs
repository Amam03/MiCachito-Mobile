using CommunityToolkit.Mvvm.ComponentModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Billete de un sorteo Tec mostrado en la pantalla "Seleccionar Billete"
/// (pantalla 13). Un registro POR billete en el carrito (a diferencia de
/// LOTENAL, que agrupa por tienda).
///
/// ESTADO MUTABLE: Agregado cambia al pulsar el botón de carrito de la
/// fila (pleca verde) y al Eliminar/Vender desde el carrito (pantallas
/// 14/15), y notifica a la UI (la pleca aparece/desaparece en vivo).
///
/// Mapeo backend (futuro): billetes_loteria (numero_billete, estatus,
/// ubicacion_actual) filtrado por id_sorteo.
/// </summary>
public partial class BilleteTec : ObservableObject
{
    /// <summary>Id del billete (mock). Backend: billetes_loteria.id_billete.</summary>
    public int IdBillete { get; init; }

    /// <summary>Sorteo al que pertenece (ver SorteosTecData).</summary>
    public int IdSorteo { get; init; }

    /// <summary>Número del billete tal como se muestra (ej. "041042").</summary>
    public string Numero { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _agregado;

    /// <summary>
    /// Texto del número formateado para la tarjeta del carrito
    /// (ej. "Boleto 041042").
    /// </summary>
    public string BoletoTexto => $"Boleto {Numero}";
}
