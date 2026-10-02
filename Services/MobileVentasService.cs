using System.Globalization;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Requests;
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

    // ── Sorteos TEC ─────────────────────────────────────────────────────────
    // Rutas propias (NO las de LN): TEC no se fracciona, se vende por BOLETO
    // COMPLETO y su precio vive en sorteos.precio_billete_completo
    // (precio_fraccion es 0.00). Ampliar el filtro de LN a categoria 4 no
    // serviria: la forma de la respuesta (dotaciones con numero_sorteo,
    // series y fracciones libres) no aplica a TEC.
    private const string RutaSorteosTec = "api/mobile/ventas/sorteos-tec";
    private const string RutaBilletesTec = "api/mobile/ventas/billetes-tec";

    /// <summary>
    /// Sorteos TEC con material ASIGNADO al billetero de la sesión.
    /// GET api/mobile/ventas/sorteos-tec.
    ///
    /// Devuelve el precio del BOLETO COMPLETO (el backend lo resuelve con
    /// normalizarPrecioBoleto) y cuántos boletos hay disponibles, para que la
    /// tarjeta 12 pueda mostrar el conteo real en vez del del mock.
    ///
    /// Los ids y precios vienen de la BD; antes salian de SorteosTecData
    /// (catálogo fijo con ids desfasados y billetes inventados).
    /// </summary>
    public async Task<IReadOnlyList<SorteoTec>> SorteosTecDisponiblesAsync(
        CancellationToken cancellationToken = default)
    {
        SorteosTecApi? api = await _api
            .GetAsync<SorteosTecApi>(RutaSorteosTec, cancellationToken)
            .ConfigureAwait(false);

        if (api?.Sorteos is null)
        {
            return Array.Empty<SorteoTec>();
        }

        return api.Sorteos.Select(s => new SorteoTec
        {
            IdSorteo = s.IdSorteo,
            NombreSorteo = s.NombreSorteo ?? string.Empty,
            // El precio llega como string decimal ("490.00").
            Precio = ParsearPrecio(s.Precio),
            ColorHex = ColorPorSorteo(s.IdSorteo, s.NombreSorteo),
        }).ToList();
    }

    /// <summary>
    /// Billetes TEC disponibles del billetero de la sesión en UN sorteo.
    /// GET api/mobile/ventas/billetes-tec?id_sorteo=
    /// </summary>
    public async Task<IReadOnlyList<BilleteTec>> BilletesTecDisponiblesAsync(
        int idSorteo, CancellationToken cancellationToken = default)
    {
        BilletesTecApi? api = await _api
            .GetAsync<BilletesTecApi>($"{RutaBilletesTec}?id_sorteo={idSorteo}", cancellationToken)
            .ConfigureAwait(false);

        if (api?.Billetes is null)
        {
            return Array.Empty<BilleteTec>();
        }

        return api.Billetes.Select(b => new BilleteTec
        {
            IdBillete = b.IdBillete,
            IdSorteo = idSorteo,
            Numero = b.NumeroBillete ?? string.Empty,
        }).ToList();
    }

    /// <summary>
    /// Crea la venta TEC (F3): POST api/mobile/ventas/crear con los
    /// id_billete EXACTOS. El backend valida pertenencia, estado 'asignado' y
    /// precio; devuelve el folio y el total REALES.
    ///
    /// Mismo endpoint que LN a proposito: la venta es la misma operacion, la
    /// unica diferencia es como el backend resuelve el precio (por linea).
    /// </summary>
    public async Task<VentaCreadaApi> CrearVentaTecAsync(
        IReadOnlyList<int> idsBilletes,
        ClienteVenta? cliente = null,
        CancellationToken cancellationToken = default)
    {
        if (idsBilletes.Count == 0)
        {
            throw new ArgumentException("La venta necesita al menos un billete.", nameof(idsBilletes));
        }

        // El cliente viaja en la MISMA peticion que los boletos. Antes se
        // escribia en la pantalla 15 pero se descartaba al confirmar: la venta
        // quedaba sin id_cliente y la columna Cliente del reporte de escritorio
        // salia vacia.
        //
        // Es opcional: si no hay datos (pantalla saltada o nombre vacio), el
        // backend registra la venta sin cliente en vez de inventar uno.
        object? payloadCliente = cliente is null
            ? null
            : new
            {
                nombre = cliente.Nombre,
                apellido_paterno = cliente.ApellidoPaterno,
                apellido_materno = cliente.ApellidoMaterno,
                telefono = cliente.Telefono,
                correo = cliente.Correo,
            };

        var payload = new
        {
            items = idsBilletes.Select(id => new { id_billete = id }).ToArray(),
            cliente = payloadCliente,
        };

        VentaCreadaApi? venta = await _api.PostAsync<VentaCreadaApi>(
            RutaCrearVenta, payload, cancellationToken).ConfigureAwait(false);

        return venta ?? throw new ApiException(
            System.Net.HttpStatusCode.InternalServerError,
            "El servidor no devolvió la venta creada.");
    }

    /// <summary>
    /// Color de la tarjeta (pantalla 12). Mantiene los colores que el usuario
    /// eligio en sep-2026 y cae a morado si aparece un sorteo nuevo, para no
    /// inventar un color por sorteo.
    /// </summary>
    private static string ColorPorSorteo(int idSorteo, string? nombre) => idSorteo switch
    {
        18 => "#F57C00",  // Sorteo Educativo (naranja)
        19 => "#00897B",  // Sorteo Dinero de X Vida (verde azulado)
        20 => "#7D44B7",  // Sorteo Mi Sueño (morado)
        21 => "#0097DC",  // Sorteo Tradicional (azul)
        _ => "#7D44B7",
    };
}
