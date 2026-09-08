using System.Collections.ObjectModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Movimiento de Premios y Reintegros: registro de la lista (mockup 3,
/// folio + fecha) cuyo detalle abre las pestanas Premios/Reintegros
/// (mockups 3.1/3.2). Fase solo-interfaz: el movimiento de ejemplo vive
/// SOLO en memoria (ver Services/PremiosReintegrosService).
/// </summary>
public sealed class MovimientoPremiosReintegros
{
    public int Folio { get; init; }

    /// <summary>Fecha del movimiento, formato mockup: "02-abril-2025".</summary>
    public string Fecha { get; init; } = string.Empty;

    public ObservableCollection<SorteoPremiosReintegros> SorteosPremios { get; } = new();

    public ObservableCollection<SorteoPremiosReintegros> SorteosReintegros { get; } = new();

    public decimal TotalPremios => SorteosPremios.Sum(s => s.Boletos.Sum(b => b.Valor));

    public decimal TotalReintegros => SorteosReintegros.Sum(s => s.Boletos.Sum(b => b.Valor));

    public decimal Total => TotalPremios + TotalReintegros;
}
