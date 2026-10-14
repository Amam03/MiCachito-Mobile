using System.Collections.ObjectModel;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Movimiento de Premios y Reintegros: registro del histórico (mockup 3,
/// folio + fecha) cuyo detalle abre las pestañas Premios/Reintegros
/// (mockups 3.1/3.2) con la vista AGRUPADA POR SORTEO (D2).
///
/// Conectado al backend (2026-10-13): la fila viene del GET
/// api/mobile/premios-reintegros; FolioTexto es el folio servidor
/// PYR-YYYYMMDD-NNNN (D3) e IdMovimiento la clave de navegación. El
/// detalle ya NO vive aquí: se consulta al backend al abrir la pantalla
/// (DetallePremiosReintegrosViewModel.CargarDetalleAsync).
/// </summary>
public sealed class MovimientoPremiosReintegros
{
    /// <summary>PK del movimiento (clave de navegación al detalle).</summary>
    public int IdMovimiento { get; init; }

    /// <summary>Folio servidor "PYR-YYYYMMDD-NNNN" (D3).</summary>
    public string FolioTexto { get; init; } = string.Empty;

    /// <summary>Compatibilidad con la UI existente (muestra el folio).</summary>
    public string Folio => FolioTexto;

    /// <summary>Fecha del movimiento, formato mockup: "02-abril-2025".</summary>
    public string Fecha { get; init; } = string.Empty;

    /// <summary>Total de premios del movimiento (del backend).</summary>
    public decimal TotalPremiosApi { get; init; }

    /// <summary>Total de reintegros del movimiento (del backend).</summary>
    public decimal TotalReintegrosApi { get; init; }

    /// <summary>Boletos del detalle (solo si ya se cargó el detalle).</summary>
    public ObservableCollection<SorteoPremiosReintegros> SorteosPremios { get; } = new();

    public ObservableCollection<SorteoPremiosReintegros> SorteosReintegros { get; } = new();

    public decimal TotalPremios => TotalPremiosApi > 0 || SorteosPremios.Count == 0
        ? TotalPremiosApi
        : SorteosPremios.Sum(s => s.Boletos.Sum(b => b.Valor));

    public decimal TotalReintegros => TotalReintegrosApi > 0 || SorteosReintegros.Count == 0
        ? TotalReintegrosApi
        : SorteosReintegros.Sum(s => s.Boletos.Sum(b => b.Valor));

    public decimal Total => TotalPremios + TotalReintegros;

    /// <summary>Rellena las colecciones agrupadas (D2) desde el detalle del backend.</summary>
    public void CargarDetalle(DetallePyRApi detalle)
    {
        SorteosPremios.Clear();
        SorteosReintegros.Clear();

        foreach (GrupoSorteoPyRApi grupo in detalle.Premios ?? new List<GrupoSorteoPyRApi>())
        {
            var sorteo = new SorteoPremiosReintegros { NombreSorteo = grupo.NombreSorteo };
            foreach (BoletoPyRApi b in grupo.Boletos ?? new List<BoletoPyRApi>())
            {
                sorteo.Boletos.Add(new BoletoPremioReintegro
                {
                    NumeroBillete = b.NumeroBillete ?? "-",
                    Signo = b.Signo ?? (b.Serie ?? "-"),
                    Vig = b.Vig ?? "-",
                    Valor = b.Valor,
                });
            }
            SorteosPremios.Add(sorteo);
        }

        foreach (GrupoSorteoPyRApi grupo in detalle.Reintegros ?? new List<GrupoSorteoPyRApi>())
        {
            var sorteo = new SorteoPremiosReintegros { NombreSorteo = grupo.NombreSorteo };
            foreach (BoletoPyRApi b in grupo.Boletos ?? new List<BoletoPyRApi>())
            {
                sorteo.Boletos.Add(new BoletoPremioReintegro
                {
                    NumeroBillete = b.NumeroBillete ?? "-",
                    Signo = b.Signo ?? (b.Serie ?? "-"),
                    Vig = b.Vig ?? "-",
                    Valor = b.Valor,
                });
            }
            SorteosReintegros.Add(sorteo);
        }
    }
}
