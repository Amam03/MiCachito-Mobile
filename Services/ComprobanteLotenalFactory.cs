using System.Text.Json;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Construye el ComprobanteLotenal imprimible (F6) con los datos
/// REALES de la operación:
/// - Venta: folio "V-...", total y boletos de la respuesta del backend
///   + producto/dotación y desglose de la sesión de venta (SesionVentaLotenal
///   sigue viva hasta que el usuario toca Continuar).
/// - Devolución: folio "MA-...", sorteo/dotación y filas del registro
///   devuelto por DevolucionService.GuardarAsync.
///
/// NUNCA repite la operación de negocio: solo lee datos ya existentes
/// para armar el texto del comprobante.
/// </summary>
public static class ComprobanteLotenalFactory
{
    /// <summary>
    /// Comprobante de VENTA: datos del carrito vendido + respuesta real
    /// del backend (folio/total) + billetero de la sesión mobile.
    /// </summary>
    public static ComprobanteLotenal DesdeVenta(
        string folio,
        decimal total,
        IReadOnlyList<int> idsBilletes)
    {
        var session = SessionServiceStatic();

        // Producto/dotación de la sesión de venta (aún viva en VentaExitosa).
        DotacionDisponible? dot = SesionVentaLotenal.Dotacion;
        string producto = dot is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(dot.NumeroSorteo)
                ? dot.NombreProducto
                : $"{dot.NombreProducto} No. {dot.NumeroSorteo}";

        // Desglose real: una línea por serie vendida (número/serie/cantidad).
        var lineas = new List<string>();
        if (idsBilletes.Count > 0 && SesionVentaLotenal.Series.Count > 0)
        {
            foreach (SerieDisponible serie in SesionVentaLotenal.Series)
            {
                int vendidos = serie.FraccionesLibres.Count(f => idsBilletes.Contains(f.IdBillete));
                if (vendidos == 0)
                {
                    continue;
                }
                string etiqueta = string.IsNullOrWhiteSpace(serie.Serie)
                    ? "SIN SERIE"
                    : $"BILLETE {serie.Numero} SERIE {serie.Serie}";
                lineas.Add($"{etiqueta} x{vendidos}");
            }

            if (lineas.Count == 0)
            {
                // Las series ya no están (p. ej. la venta limpió la sesión):
                // desglose mínimo con el total de boletos.
                lineas.Add(idsBilletes.Count == 1
                    ? "1 boleto"
                    : $"{idsBilletes.Count} boletos");
            }
        }
        else
        {
            lineas.Add(idsBilletes.Count == 1
                ? "1 boleto"
                : $"{idsBilletes.Count} boletos");
        }

        return new ComprobanteLotenal
        {
            Tipo = "COMPROBANTE DE VENTA",
            Folio = folio,
            FechaHora = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            Billetero = session?.Billetero?.NombreCompleto ?? session?.Expendio?.Usuario ?? string.Empty,
            ProductoSorteo = producto,
            Lineas = lineas,
            Total = $"${total:0.00}",
        };
    }

    /// <summary>
    /// Comprobante de DEVOLUCIÓN: datos del registro creado por
    /// DevolucionService.GuardarAsync (folio MA-..., sorteo, filas).
    /// </summary>
    public static ComprobanteLotenal DesdeDevolucion(Devolucion registro)
    {
        var session = SessionServiceStatic();

        var lineas = new List<string>();
        foreach (FilaDesglose fila in registro.Filas)
        {
            string etiqueta = string.IsNullOrWhiteSpace(fila.Serie) || fila.Serie == "-"
                ? $"BILLETE {fila.Billete}"
                : $"BILLETE {fila.Billete} {fila.Serie}";
            lineas.Add($"{etiqueta} x{fila.Cantidad}");
        }

        // Total correspondiente: cachitos devueltos de todas las filas.
        int cachitos = registro.Filas.Sum(f => f.Cantidad);

        return new ComprobanteLotenal
        {
            Tipo = "COMPROBANTE DE DEVOLUCION",
            Folio = registro.FolioTexto,
            FechaHora = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            Billetero = session?.Billetero?.NombreCompleto ?? session?.Expendio?.Usuario ?? string.Empty,
            ProductoSorteo = registro.NombreSorteo,
            Lineas = lineas,
            Total = $"{cachitos} cachito" + (cachitos == 1 ? string.Empty : "s"),
        };
    }

    /// <summary>
    /// Sesión mobile SIN inyectar el servicio (los ViewModels ya la tienen
    /// via DI; la factory es estática para usarse desde ambos flujos).
    /// </summary>
    private static SessionInfo? SessionServiceStatic()
    {
        // La sesión se persiste en SecureStorage (SessionService): para la
        // factory basta el blob persistido (mismo contenido que la sesión
        // en memoria).
        try
        {
            string? json = SecureStorage.Default.GetAsync(StorageKeys.Session).GetAwaiter().GetResult();
            return json is null
                ? null
                : JsonSerializer.Deserialize<SessionInfo>(json);
        }
        catch
        {
            return null;
        }
    }
}
