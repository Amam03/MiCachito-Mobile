using System.Collections.ObjectModel;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Estado local del flujo Premios y Reintegros (fase solo-interfaz, SIN
/// backend ni persistencia): la lista de movimientos y la captura en curso
/// viven en memoria durante la sesion.
///
/// - Lista: se siembra con UN unico movimiento de ejemplo (mockups 3/3.1/
///   3.2: folio 1, 02-abril-2025, 2 reintegros "Mayor - 3966" de $30.00)
///   solo para comprobar la navegacion; NO es un dato real.
/// - Captura: los boletos escaneados se acumulan aqui y SOLO se descartan
///   con Cancelar (mantener presionado) o se materializan en un movimiento
///   nuevo con Guardar (folio incremental) — regla del usuario: el escaneo
///   es continuo hasta una de esas dos acciones.
/// </summary>
public sealed class PremiosReintegrosService
{
    /// <summary>Valor visual del reintegro de los mockups (referencia temporal).</summary>
    public const decimal ValorReintegroEjemplo = 30.00m;

    private static readonly string[] Meses =
    {
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre",
    };

    public ObservableCollection<MovimientoPremiosReintegros> Movimientos { get; } = new();

    public ObservableCollection<BoletoCapturado> CapturaActual { get; } = new();

    public PremiosReintegrosService()
    {
        // Movimiento de ejemplo (folio 1) — unica referencia visual fija,
        // solo para comprobar el flujo. El resto nace de capturas reales
        // del usuario en la sesion.
        var sorteoMayor = new SorteoPremiosReintegros { NombreSorteo = "Mayor - 3966" };
        sorteoMayor.Boletos.Add(new BoletoPremioReintegro { NumeroBillete = "47727", Valor = ValorReintegroEjemplo });
        sorteoMayor.Boletos.Add(new BoletoPremioReintegro { NumeroBillete = "16171", Valor = ValorReintegroEjemplo });

        var ejemplo = new MovimientoPremiosReintegros { Folio = 1, Fecha = "02-abril-2025" };
        ejemplo.SorteosReintegros.Add(sorteoMayor);
        Movimientos.Add(ejemplo);
    }

    // ================= Resumen de la lista (agregado de movimientos) =================

    public decimal TotalPremios => Movimientos.Sum(m => m.TotalPremios);

    public decimal TotalReintegros => Movimientos.Sum(m => m.TotalReintegros);

    public decimal TotalGeneral => TotalPremios + TotalReintegros;

    // ================= Captura en curso =================

    public int ContadorPremios => CapturaActual.Count(b => b.Tipo == TipoCapturaBoleto.Premio);

    public int ContadorReintegros => CapturaActual.Count(b => b.Tipo == TipoCapturaBoleto.Reintegro);

    public decimal MontoPremios => CapturaActual.Where(b => b.Tipo == TipoCapturaBoleto.Premio).Sum(b => b.Valor);

    public decimal MontoReintegros => CapturaActual.Where(b => b.Tipo == TipoCapturaBoleto.Reintegro).Sum(b => b.Valor);

    /// <summary>Folio que tomara el movimiento al Guardar (incremental).</summary>
    public int FolioSiguiente => Movimientos.Count == 0 ? 1 : Movimientos.Max(m => m.Folio) + 1;

    /// <summary>Dedupe: el mismo codigo no se cuenta dos veces en la captura.</summary>
    public bool YaCapturado(string codigoCompleto) =>
        CapturaActual.Any(b => b.CodigoCompleto == codigoCompleto);

    /// <summary>Acumula un boleto escaneado en la captura en curso.</summary>
    public void AgregarBoleto(BoletoCapturado boleto)
    {
        CapturaActual.Add(boleto);
    }

    /// <summary>
    /// Guardar: convierte la captura en un movimiento nuevo (agrupado por
    /// pestaña y sorteo, tabla Billete/Signo/Vig./Valor), lo agrega a la
    /// lista con folio incremental y limpia la captura.
    /// </summary>
    public MovimientoPremiosReintegros GuardarCaptura()
    {
        var movimiento = new MovimientoPremiosReintegros
        {
            Folio = FolioSiguiente,
            Fecha = FechaDeHoy(),
        };

        foreach (var porTipo in CapturaActual.GroupBy(b => b.Tipo))
        {
            var destino = porTipo.Key == TipoCapturaBoleto.Premio
                ? movimiento.SorteosPremios
                : movimiento.SorteosReintegros;

            foreach (var porSorteo in porTipo.GroupBy(b => b.NombreSorteo))
            {
                var sorteo = new SorteoPremiosReintegros { NombreSorteo = porSorteo.Key };
                foreach (BoletoCapturado b in porSorteo)
                {
                    sorteo.Boletos.Add(new BoletoPremioReintegro
                    {
                        NumeroBillete = b.NumeroBillete,
                        Signo = b.SignoOSerie,
                        Vig = b.Vig,
                        Valor = b.Valor,
                    });
                }

                destino.Add(sorteo);
            }
        }

        Movimientos.Add(movimiento);
        CapturaActual.Clear();
        return movimiento;
    }

    /// <summary>Cancelar (mantener presionado): descarta la captura en curso.</summary>
    public void CancelarCaptura()
    {
        CapturaActual.Clear();
    }

    private static string FechaDeHoy()
    {
        DateTime hoy = DateTime.Now;
        return $"{hoy.Day:00}-{Meses[hoy.Month - 1]}-{hoy.Year:0000}";
    }
}
