using System.Globalization;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Servicio de Vender > Lotería Nacional contra la API real (F1/F2):
/// GET api/mobile/ventas/sorteos (dotaciones con material LIVE del
/// billetero de la sesión) y GET api/mobile/ventas/billetes (series
/// con fracciones libres de UNA dotación).
///
/// Todo queda acotado al billetero de la sesión (token); los
/// endpoints NO aceptan ids del cliente. El id_billetero NUNCA viaja
/// en el request — lo resuelve el backend desde la sesión.
///
/// CONSERVAR mock (regla §E): SorteosActivosLotenalData sigue en pie
/// para Devolución 6.1; este servicio NO lo toca.
///
/// Lanza ApiException en errores HTTP; la red caída llega como
/// HttpRequestException/TaskCanceled — el VM la captura y muestra el
/// error sin inventar datos (patrón SorteosService de Gestión).
/// </summary>
public class MobileVentasService
{
    private const string RutaSorteos = "api/mobile/ventas/sorteos";
    private const string RutaBilletes = "api/mobile/ventas/billetes";

    private readonly IApiClient _api;

    public MobileVentasService(IApiClient api)
    {
        _api = api;
    }

    /// <summary>
    /// Dotaciones de Lotería Nacional con material LIVE del billetero
    /// de la sesión (fecha descendente, como llega del backend).
    /// Filas sin nombre de producto NO se descartan (toda fila trae
    /// nombre del JOIN con productos).
    /// </summary>
    public async Task<IReadOnlyList<DotacionDisponible>> SorteosDisponiblesAsync(
        CancellationToken cancellationToken = default)
    {
        MobileVentasSorteosApi? api = await _api.GetAsync<MobileVentasSorteosApi>(RutaSorteos, cancellationToken)
            .ConfigureAwait(false);

        var lista = new List<DotacionDisponible>();
        if (api?.Sorteos is null)
        {
            return lista;
        }

        foreach (DotacionApi s in api.Sorteos)
        {
            lista.Add(MapearDotacion(s));
        }

        return lista;
    }

    /// <summary>
    /// Series con fracciones libres de UNA dotación del billetero
    /// (numero_sorteo null/vacío = material legacy sin número).
    /// Encabezado siempre presente; series vacías = dotación sin
    /// material LIVE hoy (todo vendido/devuelto) — la app muestra
    /// vacío y puede reintentar.
    /// </summary>
    public async Task<DetalleDotacion> BilletesDisponiblesAsync(
        int idSorteo, string? numeroSorteo = null, CancellationToken cancellationToken = default)
    {
        string path = string.IsNullOrWhiteSpace(numeroSorteo)
            ? $"{RutaBilletes}?id_sorteo={idSorteo}"
            : $"{RutaBilletes}?id_sorteo={idSorteo}&numero_sorteo={Uri.EscapeDataString(numeroSorteo)}";

        MobileVentasBilletesApi? api = await _api.GetAsync<MobileVentasBilletesApi>(path, cancellationToken)
            .ConfigureAwait(false);

        DotacionApi? encabezado = api?.Sorteo;
        var series = new List<SerieDisponible>();
        int idSerie = 1;

        if (api?.Series is not null)
        {
            foreach (SerieApi s in api.Series)
            {
                string? precioRaw = s.PrecioFraccion ?? encabezado?.PrecioFraccion;
                var fracciones = new List<FraccionLibre>();
                foreach (FraccionApi f in s.Fracciones ?? [])
                {
                    fracciones.Add(new FraccionLibre
                    {
                        IdBillete = f.IdBillete,
                        Fraccion = f.Fraccion,
                    });
                }

                series.Add(new SerieDisponible
                {
                    IdSerie = idSerie++,
                    Numero = s.NumeroBillete ?? string.Empty,
                    Serie = s.Serie ?? string.Empty,
                    Signo = string.IsNullOrWhiteSpace(s.SignoNombre) ? null : s.SignoNombre.Trim().ToUpperInvariant(),
                    FraccionesLibres = fracciones,
                    PrecioFraccion = ParsearPrecio(precioRaw),
                    TotalDisponible = s.FraccionesDisponibles,
                });
            }
        }

        DotacionDisponible dotacion = encabezado is not null
            ? MapearDotacion(encabezado)
            : new DotacionDisponible
            {
                IdSorteo = idSorteo,
                NumeroSorteo = numeroSorteo,
                NombreProducto = $"SORTEO {idSorteo}",
                PrecioFraccion = 0m,
                FraccionesDisponibles = 0,
                FechaCelebracion = null,
            };

        return new DetalleDotacion
        {
            Dotacion = dotacion,
            Series = series,
            TotalFracciones = api?.TotalFracciones ?? series.Sum(x => x.TotalDisponible),
        };
    }

    /// <summary>
    /// Mapea la fila DTO → entidad de pantalla 7.x: precio string →
    /// decimal (Invariant), fecha cruda → DateOnly es-MX seguro,
    /// Linea1/Linea2/ColorHex del catálogo de tipos de la pantalla 6
    /// (SorteosLotenalData, colores reales del mockup; catálogo
    /// real: pendiente v2 #9).
    /// </summary>
    private static DotacionDisponible MapearDotacion(DotacionApi s)
    {
        DateOnly? fecha = FormatosFecha.TryParseFechaBackend(s.FechaSorteo, out DateOnly f)
            ? f
            : null;

        // Presentación del catálogo pantalla 6: match EXACTO sin el
        // prefijo "SORTEO " ("MAYOR" → SORTEO/MAYOR #FFB600; "ZODIACO
        // ESPECIAL" → SORTEO ZODIACO/ESPECIAL). Sin match (producto
        // nuevo sin catálogo, ej. GORDITO NAVIDEÑO) → defaults neutros.
        string nombre = (s.NombreProducto ?? string.Empty).Trim().ToUpperInvariant();
        SorteoLotenal? tipo = SorteosLotenalData.Sorteos.FirstOrDefault(t =>
        {
            string nombreTipo = t.Nombre.StartsWith("SORTEO ", StringComparison.OrdinalIgnoreCase)
                ? t.Nombre["SORTEO ".Length..]
                : t.Nombre;
            return string.Equals(nombreTipo, nombre, StringComparison.OrdinalIgnoreCase);
        });

        return new DotacionDisponible
        {
            IdSorteo = s.IdSorteo,
            NumeroSorteo = string.IsNullOrWhiteSpace(s.NumeroSorteo) ? null : s.NumeroSorteo.Trim(),
            NombreProducto = nombre,
            PrecioFraccion = ParsearPrecio(s.PrecioFraccion),
            FraccionesDisponibles = s.FraccionesDisponibles,
            FechaCelebracion = fecha,
            Linea1 = tipo?.Linea1 ?? "SORTEO",
            Linea2 = tipo?.Linea2 ?? nombre,
            ColorHex = tipo?.ColorHex ?? "#4125F4",
        };
    }

    /// <summary>
    /// Precio string del backend ("70.00") → decimal Invariant.
    /// Nulo/inválido → 0 (sin inventar; la fila sigue visible).
    /// </summary>
    private static decimal ParsearPrecio(string? raw) =>
        decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal precio)
            ? precio
            : 0m;

    /// <summary>
    /// Crea la venta LOTENAL del billetero de la sesión (F3):
    /// POST api/mobile/ventas/crear con los id_billete EXACTOS
    /// seleccionados (uno por fracción a vender). El backend resuelve
    /// el vendedor (sesión), el precio (sorteos.precio_fraccion) y
    /// fija id_tienda NULL; la respuesta trae folio/total reales.
    /// Errores HTTP (409 vendido, 403 ajeno, 422 validación, 401 sin
    /// sesión) llegan como ApiException con el mensaje del backend.
    /// </summary>
    public async Task<VentaCreadaApi> CrearVentaAsync(
        IReadOnlyList<int> idsBilletes, CancellationToken cancellationToken = default)
    {
        if (idsBilletes.Count == 0)
        {
            throw new ArgumentException("La venta necesita al menos un billete.", nameof(idsBilletes));
        }

        var payload = new
        {
            items = idsBilletes.Select(id => new { id_billete = id }).ToArray(),
        };

        VentaCreadaApi? venta = await _api.PostAsync<VentaCreadaApi>(
            RutaCrearVenta, payload, cancellationToken).ConfigureAwait(false);

        return venta ?? throw new ApiException(
            System.Net.HttpStatusCode.InternalServerError,
            "El servidor no devolvió la venta creada.");
    }

    private const string RutaCrearVenta = "api/mobile/ventas/crear";

    /// <summary>
    /// Devuelve billetes LOTENAL asignados al billetero de la sesión (F4):
    /// POST api/mobile/ventas/devolver con los campos del QR parseado de
    /// cada boleto escaneado (numero_billete, serie, fraccion, signo y
    /// numero_sorteo/dotación). El backend valida pertenencia al billetero
    /// (403 ajeno), estado 'asignado' (409 vendido/devuelto), guard de
    /// dotación cerrada (409) y crea el movimiento 'pendiente' con estado
    /// LIVE inmediato (devuelto/devolucion/billetero NULL) — la
    /// liquidación administrativa queda en el flujo Desktop existente.
    /// </summary>
    public async Task<DevolucionCreadaApi> DevolverAsync(
        IReadOnlyList<ItemDevolucionApi> items, CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("La devolución necesita al menos un boleto.", nameof(items));
        }

        var payload = new { items };

        DevolucionCreadaApi? dev = await _api.PostAsync<DevolucionCreadaApi>(
            RutaDevolver, payload, cancellationToken).ConfigureAwait(false);

        return dev ?? throw new ApiException(
            System.Net.HttpStatusCode.InternalServerError,
            "El servidor no devolvió la devolución creada.");
    }

    private const string RutaDevolver = "api/mobile/ventas/devolver";
}
