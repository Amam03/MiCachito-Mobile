using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

#if ANDROID
using MiCachito.Mobile.Platforms.Android.Services;
#endif

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// VM del "Detalle de Pago" (mockups 8.1/8.2): header con Folio, Fecha y
/// Total del MOVIMIENTO, desglose con filas "Descripción: / Referencia: /
/// Monto: $ X" (formas de pago positivas + documentos de cartera pagados
/// negativos "FACTURA LN") y FAB de descarga con overlay "Descargando..."
/// + aviso verde de guardado.
///
/// Conectado al backend real (GET api/mobile/pagos/recibo/{id}): el
/// controlador arma las filas del desglose desde las observaciones JSON
/// que escribe Escritorio al capturar el cobro en "B. Recepción de
/// Pagos" (pagos_service.guardar_movimiento) — una ficha por forma de
/// pago, agrupadas por folio_grupo — y las replica con el formato del
/// mockup 8.1. Fichas sin desglose degradan a forma de pago simple.
/// </summary>
public partial class DetallePagoViewModel : ObservableObject, IQueryAttributable
{
    private readonly RecibosPagoService _recibos;
    private readonly ReciboPagoPdfService _pdf;

    private ReciboPagoDetalleApi? _detalle;

    /// <summary>Id de la ficha recibido por query string (patrón del proyecto).</summary>
    public string? FolioQuery { get; set; }

    [ObservableProperty]
    private bool descargando;

    [ObservableProperty]
    private bool descargaExitosa;

    [ObservableProperty]
    private string? rutaPdf;

    /// <summary>True mientras carga el detalle (overlay "Cargando...").</summary>
    [ObservableProperty]
    private bool cargando;

    /// <summary>Mensaje de error de carga (sin conexión / HTTP).</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    /// <summary>True cuando hay mensaje de error (muestra el aviso rojo).</summary>
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    public ReciboPagoDetalleApi? Detalle => _detalle;

    // Propiedades PLANAS (patrón pantalla-9): el source-gen de bindings
    // compilados no admite paths sobre entidad nullable sin CS8603.
    public string FolioTexto => _detalle is not null ? $"Folio: {_detalle.FolioFicha}" : string.Empty;

    /// <summary>Total del MOVIMIENTO (header del mockup 8.1).</summary>
    public string TotalTexto => _detalle is not null
        ? $"${_detalle.TotalMovimiento.ToString("N2", CultureInfo.CurrentCulture)}"
        : string.Empty;

    public string FechaTexto
    {
        get
        {
            if (_detalle is null)
            {
                return string.Empty;
            }
            string? cruda = _detalle.FechaAplicacion ?? _detalle.FechaPago;
            if (Helpers.FormatosFecha.TryParseFechaBackend(cruda, out DateOnly fecha))
            {
                return Helpers.FormatosFecha.FechaLarga(fecha);
            }
            return cruda ?? string.Empty;
        }
    }

    /// <summary>
    /// Desglose de la operación (mockup 8.1): filas construidas por el
    /// backend — formas de pago (positivas) seguidas de documentos de
    /// cartera pagados (negativos "FACTURA LN").
    /// </summary>
    public IReadOnlyList<MovimientoPago> Desglose { get; private set; } = Array.Empty<MovimientoPago>();

    public string Title => "Detalles de Pago";

    /// <summary>True cuando hay un recibo cargado (controla el FAB).</summary>
    public bool HayRecibo => _detalle is not null;

    /// <summary>Regresa a la lista de recibos.</summary>
    [RelayCommand]
    private Task GoBackAsync()
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync("..");
        }
        return Task.CompletedTask;
    }

    public DetallePagoViewModel(RecibosPagoService recibos, ReciboPagoPdfService pdf)
    {
        _recibos = recibos;
        _pdf = pdf;
    }

    /// <summary>Carga el detalle por id de ficha (patrón AlAparecer + QueryProperty).</summary>
    public async Task AlAparecerAsync()
    {
        if (Cargando)
        {
            return;
        }

        long idFicha = long.TryParse(FolioQuery, out long id) ? id : 0;
        if (idFicha <= 0)
        {
            MensajeError = "Recibo no especificado";
            OnPropertyChanged(nameof(HayError));
            _detalle = null;
            NotificarRecibo();
            return;
        }

        Cargando = true;
        MensajeError = string.Empty;
        try
        {
            _detalle = await _recibos.ObtenerDetalleAsync(idFicha);
            if (_detalle is null)
            {
                MensajeError = "El recibo no está disponible";
                OnPropertyChanged(nameof(HayError));
            }
            else
            {
                Desglose = ConstruirDesglose(_detalle);
            }
        }
        catch (TaskCanceledException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (OperationCanceledException)
        {
            MensajeError = "Consulta cancelada";
            OnPropertyChanged(nameof(HayError));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (ApiException ex)
        {
            // 404: ficha ajena, pendiente o inexistente — mensaje del backend.
            MensajeError = ex.Message;
            OnPropertyChanged(nameof(HayError));
        }
        finally
        {
            Cargando = false;
        }

        NotificarRecibo();
    }

    /// <summary>Notifica todas las propiedades planas derivadas del recibo.</summary>
    private void NotificarRecibo()
    {
        OnPropertyChanged(nameof(Detalle));
        OnPropertyChanged(nameof(HayRecibo));
        OnPropertyChanged(nameof(FolioTexto));
        OnPropertyChanged(nameof(TotalTexto));
        OnPropertyChanged(nameof(FechaTexto));
        OnPropertyChanged(nameof(Desglose));
        Descargando = false;
        DescargaExitosa = false;
    }

    /// <summary>
    /// Construye las filas del desglose (mockup 8.1) desde las filas que
    /// armó el backend:
    ///   - Formas de pago del movimiento (positivas, "Descripción: X /
    ///     Referencia: Y / Monto: $ Z").
    ///   - Documentos de cartera pagados (negativos, Descripción
    ///     "FACTURA LN", Referencia "NUMSORTEO --> FOLIO X").
    /// Fichas sin desglose: una sola fila con la forma de la ficha.
    /// </summary>
    private static IReadOnlyList<MovimientoPago> ConstruirDesglose(ReciboPagoDetalleApi d)
    {
        var filas = new List<MovimientoPago>();

        if (d.Formas is { Count: > 0 })
        {
            foreach (FilaDesgloseApi forma in d.Formas)
            {
                filas.Add(new MovimientoPago
                {
                    Descripcion = forma.Descripcion,
                    Referencia = forma.Referencia,
                    Monto = forma.Monto,
                });
            }
        }
        else
        {
            // Ficha sembrada sin desglose: degrada a la forma de la propia ficha.
            filas.Add(new MovimientoPago
            {
                Descripcion = EtiquetaTipoPago(d.TipoPago),
                Referencia = ReferenciaFormaPago(d),
                Monto = d.Monto,
            });
        }

        if (d.Documentos is { Count: > 0 })
        {
            foreach (DocumentoPagoApi doc in d.Documentos)
            {
                filas.Add(new MovimientoPago
                {
                    // Label del mockup 8.1 para documentos de cartera pagados.
                    Descripcion = "FACTURA LN",
                    Referencia = doc.Referencia.Length > 0
                        ? doc.Referencia
                        : doc.FolioDocumento,
                    Monto = -doc.MontoPago,
                });
            }
        }

        return filas;
    }

    /// <summary>Etiqueta de la forma de pago de la ficha.</summary>
    private static string EtiquetaTipoPago(string tipo) => tipo switch
    {
        "efectivo" => "Efectivo",
        "transferencia" => "Transferencia",
        "cheque" => "Cheque",
        "deposito" => "Depósito",
        "tarjeta" => "Tarjeta",
        _ => string.IsNullOrEmpty(tipo) ? "Pago" : Capitalizar(tipo),
    };

    /// <summary>Referencia de la forma de pago: referencia bancaria de la ficha o del detalle de depósitos.</summary>
    private static string ReferenciaFormaPago(ReciboPagoDetalleApi d)
    {
        if (!string.IsNullOrEmpty(d.ReferenciaBancaria))
        {
            return d.ReferenciaBancaria;
        }
        // Transferencias del movimiento compuesto: la primera coincidencia
        // por monto aporta su referencia operativa (folio del movimiento).
        if (d.TipoPago == "transferencia" &&
            d.DepositosTransferenciasDetalle is { Count: > 0 } detalle)
        {
            MovimientoPago? match = detalle
                .Where(x => x.Monto == d.Monto && x.Referencia.Length > 0)
                .Select(x => new MovimientoPago
                {
                    Descripcion = x.TipoPago,
                    Referencia = x.Referencia.Length > 0 ? $"Ref: {x.Referencia}" : x.FolioMovimiento,
                    Monto = x.Monto,
                })
                .FirstOrDefault();
            if (match is not null)
            {
                return match.Referencia;
            }
        }
        return d.TipoPago.ToUpperInvariant();
    }

    private static string Capitalizar(string texto) =>
        string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0]) + texto[1..];

    void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("folio", out object? v) && v is string s)
        {
            FolioQuery = s;
        }
    }

    /// <summary>
    /// Descarga el comprobante: overlay "Descargando..." (mockup 8.2),
    /// genera el PDF real con los datos del backend, lo copia a
    /// Descargas y muestra el aviso verde "Archivo guardado en la
    /// carpeta de Descargas".
    /// </summary>
    [RelayCommand]
    private async Task DescargarAsync()
    {
        if (Descargando || _detalle is null)
        {
            return;
        }

        Descargando = true;
        DescargaExitosa = false;
        try
        {
            ReciboPago recibo = ConstruirReciboPdf(_detalle);
            string ruta = await _pdf.GenerarAsync(recibo);
            RutaPdf = ruta;

#if ANDROID
            string? guardado = DescargasService.GuardarEnDescargas(
                ruta, recibo.NombreArchivoPdf + ".pdf");
            DescargaExitosa = guardado is not null;
#else
            DescargaExitosa = true;
#endif
        }
        finally
        {
            Descargando = false;
        }
    }

    /// <summary>
    /// Adapta el detalle del backend a la entidad del PDF (mockup 8.3):
    /// Total = total del movimiento; Documentos Pagados = filas de
    /// documentos del desglose (referencia "NUMSORTEO --> FOLIO X");
    /// Formas de Pago según el desglose crudo del movimiento (si existe)
    /// o la forma de la ficha.
    /// </summary>
    private static ReciboPago ConstruirReciboPdf(ReciboPagoDetalleApi d)
    {
        Helpers.FormatosFecha.TryParseFechaBackend(d.FechaAplicacion ?? d.FechaPago, out DateOnly fechaPago);
        var fecha = fechaPago == default ? DateTime.Today : fechaPago.ToDateTime(TimeOnly.MinValue);

        var documentos = new List<MovimientoPago>();
        if (d.Documentos is { Count: > 0 })
        {
            documentos.AddRange(d.Documentos.Select(doc => new MovimientoPago
            {
                Descripcion = "FACTURA LN",
                Referencia = doc.Referencia.Length > 0
                    ? doc.Referencia
                    : doc.FolioDocumento,
                Monto = -doc.MontoPago,
            }));
        }

        return new ReciboPago
        {
            Folio = d.FolioFicha,
            Fecha = fecha,
            Total = d.TotalMovimiento,
            Cliente = d.Cliente,
            // nombre_cedis YA trae el prefijo "CEDIS " desde la BD
            // ("CEDIS Puebla") — no se antepone de nuevo (auditoría
            // 2026-09-21: el PDF salía "(CEDIS CEDIS Puebla)").
            Cedis = d.Cedis,
            Desglose = documentos,
            DocumentosPagados = documentos,
            FormasPago = ConstruirFormasPagoPdf(d),
        };
    }

    /// <summary>
    /// Tabla "Formas de Pago" del PDF (mockup 8.3: 7 filas fijas — no hay
    /// fila "Lotería Nacional"). Con desglose del movimiento usa sus
    /// totales reales: loteria_nacional_total se suma a la fila
    /// "Depósitos/Transferencias" porque así lo registra Escritorio (la
    /// ficha de LN se crea con tipo_pago 'deposito' en
    /// pagos_service.guardar_movimiento) — así el Total de la tabla
    /// cuadra con el del movimiento. Sin desglose (fichas sembradas)
    /// degrada a la forma de la ficha con el resto en $0.00.
    /// </summary>
    private static List<(string Forma, decimal Monto)> ConstruirFormasPagoPdf(ReciboPagoDetalleApi d)
    {
        decimal efectivo = 0m, depositos = 0m, instantanea = 0m, cheques = 0m, notas = 0m;

        if (d.Desglose is { Count: > 0 } desglose)
        {
            efectivo = Valor(desglose, "efectivo");
            depositos = Valor(desglose, "depositos_transferencias_total")
                      + Valor(desglose, "loteria_nacional_total");
            instantanea = Valor(desglose, "loteria_instantanea_total");
            cheques = Valor(desglose, "cheques_total");
            notas = Valor(desglose, "notas_credito_total");
        }
        else
        {
            switch (d.TipoPago)
            {
                case "efectivo": efectivo = d.Monto; break;
                case "transferencia":
                case "deposito": depositos = d.Monto; break;
                case "cheque": cheques = d.Monto; break;
                default: efectivo = d.Monto; break;
            }
        }

        return new List<(string, decimal)>
        {
            ("Efectivo", efectivo),
            ("Depósitos/Transferencias", depositos),
            ("Premios", 0m),
            ("Reintegros", 0m),
            ("Lotería Instántanea", instantanea),
            ("Cheques", cheques),
            ("Notas de Crédito", notas),
        };
    }

    private static decimal Valor(Dictionary<string, decimal> desglose, string clave) =>
        desglose.TryGetValue(clave, out decimal v) ? v : 0m;

    /// <summary>Abre/visualiza el comprobante con el visor del sistema.</summary>
    [RelayCommand]
    private async Task AbrirComprobanteAsync()
    {
        if (string.IsNullOrEmpty(RutaPdf) || !System.IO.File.Exists(RutaPdf))
        {
            return;
        }

        try
        {
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(RutaPdf),
                Title = "Comprobante de Pago",
            });
        }
        catch
        {
            // Fallback: diálogo de compartir si no hay visor (emulador)
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Comprobante de Pago",
                File = new ShareFile(RutaPdf),
            });
        }
    }
}
