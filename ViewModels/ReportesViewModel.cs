using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// VM de Reportes (mockups 9.x). Pestañas: Estado de Cuenta, Fondo de
/// Ahorro y Facturación. FASE 2 (2026-09-22): Fondo de Ahorro consulta
/// el backend real (GET api/mobile/reportes/fondo-ahorro) con overlay de
/// carga, manejo de errores (aviso rojo en el modal, que queda abierto
/// para reintentar) y serie de saldo acumulado en la gráfica. FASE 3
/// (2026-09-22): Facturación consulta el backend real (GET
/// api/mobile/reportes/facturacion) con el mismo patrón. FASE 4
/// (2026-09-22): Estado de Cuenta consulta el backend real (GET
/// api/mobile/reportes/estado-cuenta) con overlay, aviso rojo sobre la
/// pestaña y tarjetas de sorteo reales (solo consignaciones, semántica
/// Fase 1).
/// </summary>
public partial class ReportesViewModel : BaseViewModel
{
    private readonly EstadoCuentaService _servicio;
    private readonly EstadoDeCuentaPdfService _pdf;
    private readonly FondoAhorroService _fondoServicio;
    private readonly FondoAhorroPdfService _fondoPdf;
    private readonly FacturacionService _facServicio;
    private readonly FacturacionPdfService _facPdf;

    /// <summary>Reporte Estado de Cuenta generado (null hasta generar).</summary>
    private EstadoCuenta? _estadoCuenta;

    /// <summary>Reporte Fondo de Ahorro generado (null hasta consultar).</summary>
    private FondoAhorro? _fondoAhorro;

    /// <summary>Reporte de Facturación generado (null hasta consultar).</summary>
    private Facturacion? _facturacion;

    // ── Pestañas / overlays ──

    /// <summary>Pestaña activa (0=Estado de Cuenta, 1=Fondo de Ahorro, 2=Facturación).</summary>
    [ObservableProperty]
    private int _pestanaActiva;

    /// <summary>True mientras corre "Procesando...." (Estado de Cuenta).</summary>
    [ObservableProperty]
    private bool _procesando;

    /// <summary>True mientras corre "Descargando....".</summary>
    [ObservableProperty]
    private bool _descargando;

    /// <summary>True mientras la consulta de Fondo de Ahorro está en vuelo (overlay).</summary>
    [ObservableProperty]
    private bool _consultandoFondo;

    /// <summary>Mensaje de error de la consulta de Fondo de Ahorro (aviso rojo en el modal).</summary>
    [ObservableProperty]
    private string _mensajeErrorFondo = string.Empty;

    /// <summary>True cuando hay error de consulta de Fondo de Ahorro (muestra el aviso).</summary>
    public bool HayErrorFondo => !string.IsNullOrEmpty(MensajeErrorFondo);

    /// <summary>Mensaje de error de la generación de Estado de Cuenta (aviso rojo).</summary>
    [ObservableProperty]
    private string _mensajeErrorEc = string.Empty;

    /// <summary>True cuando hay error de generación de Estado de Cuenta (muestra el aviso).</summary>
    public bool HayErrorEc => !string.IsNullOrEmpty(MensajeErrorEc);

    /// <summary>Notifica las derivadas de error de Estado de Cuenta.</summary>
    private void NotificarErrorEc()
    {
        OnPropertyChanged(nameof(HayErrorEc));
    }

    /// <summary>True mientras la consulta de Facturación está en vuelo (overlay).</summary>
    [ObservableProperty]
    private bool _consultandoFac;

    /// <summary>Mensaje de error de la consulta de Facturación (aviso rojo en el modal).</summary>
    [ObservableProperty]
    private string _mensajeErrorFac = string.Empty;

    /// <summary>True cuando hay error de consulta de Facturación.</summary>
    public bool HayErrorFac => !string.IsNullOrEmpty(MensajeErrorFac);

    /// <summary>Error visible en el modal compartido: el de la pestaña activa (FA o Facturación).</summary>
    public bool ModalHayError => PestanaActiva == 2 ? HayErrorFac : HayErrorFondo;

    /// <summary>Mensaje de error del modal compartido: el de la pestaña activa.</summary>
    public string ModalMensajeError => PestanaActiva == 2 ? MensajeErrorFac : MensajeErrorFondo;

    /// <summary>Notifica las derivadas de error del modal compartido (FA/FAC).</summary>
    private void NotificarErrorModal()
    {
        OnPropertyChanged(nameof(ModalHayError));
        OnPropertyChanged(nameof(ModalMensajeError));
    }

    // ── Estado de Cuenta ──

    /// <summary>True cuando el reporte de Estado de Cuenta ya fue generado.</summary>
    [ObservableProperty]
    private bool _reporteGenerado;

    /// <summary>True tras completar la descarga del Estado de Cuenta.</summary>
    [ObservableProperty]
    private bool _reporteDescargado;

    /// <summary>Ruta absoluta del PDF de Estado de Cuenta.</summary>
    [ObservableProperty]
    private string? _rutaPdf;

    // ── Fondo de Ahorro ──

    /// <summary>True cuando el modal "Seleccionar Fechas" está abierto (FA o Facturación).</summary>
    [ObservableProperty]
    private bool _modalFechasVisible;

    /// <summary>Fecha Desde elegida en el modal (FA o Facturación; null hasta seleccionar).</summary>
    [ObservableProperty]
    private DateTime? _fechaDesde;

    /// <summary>Fecha Hasta elegida en el modal (FA o Facturación; null hasta seleccionar).</summary>
    [ObservableProperty]
    private DateTime? _fechaHasta;

    /// <summary>Texto del campo Desde (dd/MM/yyyy o vacío).</summary>
    [ObservableProperty]
    private string _fechaDesdeTexto = string.Empty;

    /// <summary>Texto del campo Hasta (dd/MM/yyyy o vacío).</summary>
    [ObservableProperty]
    private string _fechaHastaTexto = string.Empty;

    /// <summary>True cuando el reporte de Fondo de Ahorro está en pantalla.</summary>
    [ObservableProperty]
    private bool _fondoConsultado;

    /// <summary>True tras completar la descarga del Fondo de Ahorro.</summary>
    [ObservableProperty]
    private bool _fondoDescargado;

    /// <summary>Ruta absoluta del PDF de Fondo de Ahorro.</summary>
    [ObservableProperty]
    private string? _rutaPdfFondo;

    // ── Facturación ──

    /// <summary>Fecha Desde propia de Facturación (independiente de FA).</summary>
    [ObservableProperty]
    private DateTime? _facFechaDesde;

    /// <summary>Fecha Hasta propia de Facturación (independiente de FA).</summary>
    [ObservableProperty]
    private DateTime? _facFechaHasta;

    /// <summary>Texto del campo Desde de Facturación.</summary>
    [ObservableProperty]
    private string _facDesdeTexto = string.Empty;

    /// <summary>Texto del campo Hasta de Facturación.</summary>
    [ObservableProperty]
    private string _facHastaTexto = string.Empty;

    /// <summary>True cuando el reporte de Facturación está en pantalla.</summary>
    [ObservableProperty]
    private bool _facConsultado;

    /// <summary>True tras completar la descarga de Facturación.</summary>
    [ObservableProperty]
    private bool _facDescargado;

    /// <summary>Ruta absoluta del PDF de Facturación.</summary>
    [ObservableProperty]
    private string? _rutaPdfFac;

    /// <summary>Se dispara cuando el pie necesita redibujarse (nueva consulta).</summary>
    public event EventHandler? PieSolicitaRedibujo;

    /// <summary>Se dispara cuando la gráfica FA necesita redibujarse (nueva consulta).</summary>
    public event EventHandler? GraficaFaSolicitaRedibujo;

    public ReportesViewModel(
        EstadoCuentaService servicio,
        EstadoDeCuentaPdfService pdf,
        FondoAhorroService fondoServicio,
        FondoAhorroPdfService fondoPdf,
        FacturacionService facServicio,
        FacturacionPdfService facPdf)
    {
        _servicio = servicio;
        _pdf = pdf;
        _fondoServicio = fondoServicio;
        _fondoPdf = fondoPdf;
        _facServicio = facServicio;
        _facPdf = facPdf;
        Title = "Reportes";
    }

    // ── Propiedades derivadas (patrón del proyecto: sin converters) ──

    /// <summary>Reporte Estado de Cuenta en pantalla.</summary>
    public EstadoCuenta? EstadoCuenta
    {
        get => _estadoCuenta;
        private set => SetProperty(ref _estadoCuenta, value);
    }

    /// <summary>True cuando hay reporte de Estado de Cuenta en pantalla.</summary>
    public bool HayReporte => _estadoCuenta is not null && ReporteGenerado;

    /// <summary>True cuando NO hay reporte de Estado de Cuenta.</summary>
    public bool SinReporte => !HayReporte;

    /// <summary>True si la pestaña Estado de Cuenta está activa.</summary>
    public bool Pestana0Activa => PestanaActiva == 0;

    /// <summary>True si la pestaña Fondo de Ahorro está activa.</summary>
    public bool Pestana1Activa => PestanaActiva == 1;

    /// <summary>True si la pestaña Facturación está activa.</summary>
    public bool Pestana2Activa => PestanaActiva == 2;

    /// <summary>Iconos de las pestañas (azul la activa, gris las demás).</summary>
    public string IconoPestana0 => Pestana0Activa ? "icon_grafica_azul.svg" : "icon_grafica_gris.svg";
    public string IconoPestana1 => Pestana1Activa ? "icon_alcancia_azul.svg" : "icon_alcancia_gris.svg";
    public string IconoPestana2 => Pestana2Activa ? "icon_recibo_azul.svg" : "icon_recibo_gris.svg";

    /// <summary>Color del label de cada pestaña (azul la activa, gris las demás).</summary>
    public string ColorPestana0 => Pestana0Activa ? "#4125F4" : "#9E9E9E";
    public string ColorPestana1 => Pestana1Activa ? "#4125F4" : "#9E9E9E";
    public string ColorPestana2 => Pestana2Activa ? "#4125F4" : "#9E9E9E";

    /// <summary>True si el listado de sorteos está vacío (sin consignaciones vivas).</summary>
    public bool SinSorteos => EstadoCuenta?.Sorteos.Count == 0;

    /// <summary>Filas de sorteos del Estado de Cuenta (tarjetas, BindableLayout).</summary>
    public IReadOnlyList<SorteoEstadoCuenta> SorteosEc =>
        (IReadOnlyList<SorteoEstadoCuenta>?)EstadoCuenta?.Sorteos ?? new List<SorteoEstadoCuenta>();


    // ── Valores planos del Estado de Cuenta (bindings compilados) ──

    public decimal FondoDeAhorro => EstadoCuenta?.FondoDeAhorro ?? 0m;
    public decimal Fideicomiso => EstadoCuenta?.Fideicomiso ?? 0m;
    public decimal Pagares => EstadoCuenta?.Pagares ?? 0m;
    public decimal BolsaElectronica => EstadoCuenta?.BolsaElectronica ?? 0m;
    public decimal GarantiaTotal => EstadoCuenta?.GarantiaTotal ?? 0m;
    public decimal CapacidadDeCredito => EstadoCuenta?.CapacidadDeCredito ?? 0m;
    public decimal VencidoTotal => EstadoCuenta?.VencidoTotal ?? 0m;
    public decimal ConsignaTotal => EstadoCuenta?.ConsignaTotal ?? 0m;
    public decimal PagosTotal => EstadoCuenta?.PagosTotal ?? 0m;
    public decimal TotalFinal => EstadoCuenta?.TotalFinal ?? 0m;

    // ── Fondo de Ahorro: derivadas ──

    /// <summary>Periodo formateado "01-octubre-2025 / 29-octubre-2025" (o vacío).</summary>
    public string PeriodoTexto
    {
        get
        {
            if (_fondoAhorro is null)
            {
                return string.Empty;
            }
            return $"{FormatoLargo(_fondoAhorro.FechaInicio)} / {FormatoLargo(_fondoAhorro.FechaFin)}";
        }
    }

    /// <summary>Fecha Desde formateada "01-octubre-2025" (o vacía).</summary>
    public string FechaDesdeLarga => FechaDesde is null ? string.Empty : FormatoLargo(FechaDesde.Value);

    /// <summary>Fecha Hasta formateada "29-octubre-2025" (o vacía).</summary>
    public string FechaHastaLarga => FechaHasta is null ? string.Empty : FormatoLargo(FechaHasta.Value);

    /// <summary>True si ambas fechas están seleccionadas (habilita Consultar).</summary>
    public bool PuedeConsultar => FechaDesde is not null && FechaHasta is not null;

    /// <summary>True si NO se ha consultado el Fondo de Ahorro (muestra el campo Periodo).</summary>
    public bool SinFondo => !FondoConsultado;

    // ── Facturación: derivadas ──

    /// <summary>True si NO se ha consultado Facturación (muestra el campo Periodo).</summary>
    public bool SinFac => !FacConsultado;

    /// <summary>Texto del campo Desde del modal según la pestaña activa.</summary>
    public string ModalDesdeTexto => PestanaActiva == 2 ? FacDesdeTexto : FechaDesdeTexto;

    /// <summary>Texto del campo Hasta del modal según la pestaña activa.</summary>
    public string ModalHastaTexto => PestanaActiva == 2 ? FacHastaTexto : FechaHastaTexto;

    /// <summary>True si el modal puede consultar (ambas fechas de la pestaña activa).</summary>
    public bool ModalPuedeConsultar => PestanaActiva == 2
        ? FacFechaDesde is not null && FacFechaHasta is not null
        : FechaDesde is not null && FechaHasta is not null;

    /// <summary>Fechas largas de Facturación ("08-septiembre-2026").</summary>
    public string FacDesdeLarga => FacFechaDesde is null ? string.Empty : FormatoLargo(FacFechaDesde.Value);
    public string FacHastaLarga => FacFechaHasta is null ? string.Empty : FormatoLargo(FacFechaHasta.Value);

    /// <summary>Total facturado del periodo (dinámico).</summary>
    public decimal FacTotal => _facturacion?.TotalFacturado ?? 0m;

    /// <summary>Porcentaje principal: el de la categoría con mayor venta (dinámico).</summary>
    public double FacPorcentajePrincipal => _facturacion?.Categorias.OrderByDescending(c => c.Monto).FirstOrDefault()?.Porcentaje ?? 0;

    /// <summary>Categorías del periodo (para el pie y la leyenda).</summary>
    public IReadOnlyList<CategoriaFacturacion> FacCategorias =>
        (IReadOnlyList<CategoriaFacturacion>?)_facturacion?.Categorias ?? new List<CategoriaFacturacion>();

    /// <summary>Segmentos del pie (nombre, color, porcentaje).</summary>
    public IReadOnlyList<(string Nombre, string ColorHex, double Porcentaje)> FacSegmentos =>
        FacCategorias.Select(c => (c.Nombre, c.ColorHex, c.Porcentaje)).ToList();

    /// <summary>Aviso verde Facturación (pestaña + descargado).</summary>
    public bool AvisoFacVisible => Pestana2Activa && FacDescargado && !Descargando;

    /// <summary>FAB descarga Facturación: pestaña activa + consultado.</summary>
    public bool FabDescargaFacVisible => Pestana2Activa && FacConsultado;

    /// <summary>FAB PDF Facturación: pestaña activa + descargado.</summary>
    public bool FabPdfFacVisible => Pestana2Activa && FacDescargado;

    /// <summary>True si el aviso verde de descarga debe verse (pestaña FA + descargado).</summary>
    public bool AvisoFondoVisible => Pestana1Activa && FondoDescargado && !Descargando;

    /// <summary>FAB descarga FA: pestaña activa + consultado.</summary>
    public bool FabDescargaFaVisible => Pestana1Activa && FondoConsultado;

    /// <summary>FAB PDF FA: pestaña activa + descargado.</summary>
    public bool FabPdfFaVisible => Pestana1Activa && FondoDescargado;

    /// <summary>Serie de la gráfica FA: saldo acumulado por fecha (punto inicial = saldo_inicial).</summary>
    public IReadOnlyList<(double Fraccion, decimal Saldo)> FaSerieSaldo
    {
        get
        {
            if (_fondoAhorro is null || _fondoAhorro.Movimientos.Count == 0)
            {
                return Array.Empty<(double, decimal)>();
            }

            DateTime inicio = _fondoAhorro.FechaInicio;
            DateTime fin = _fondoAhorro.FechaFin;
            double span = Math.Max(1.0, (fin - inicio).TotalDays);

            decimal saldo = _fondoAhorro.SaldoInicial;
            var serie = new List<(double, decimal)> { (0.0, saldo) };
            foreach (MovimientoFondoAhorro m in _fondoAhorro.Movimientos)
            {
                saldo += m.Monto; // aportación + / retiro −
                double fraccion = Math.Clamp((m.Fecha - inicio).TotalDays / span, 0.0, 1.0);
                serie.Add((fraccion, saldo));
            }

            return serie;
        }
    }

    /// <summary>FAB descarga Estado de Cuenta: pestaña activa + generado.</summary>
    public bool FabDescargaEcVisible => Pestana0Activa && HayReporte;

    /// <summary>FAB PDF Estado de Cuenta: pestaña activa + descargado.</summary>
    public bool FabPdfEcVisible => Pestana0Activa && ReporteDescargado;

    /// <summary>Opacidad del botón Consultar (0.5 deshabilitado, 1 habilitado).</summary>
    public float OpacidadConsultar => ModalPuedeConsultar ? 1f : 0.5f;

    /// <summary>Valores planos del Fondo de Ahorro (bindings compilados).</summary>
    public decimal FaSaldoInicial => _fondoAhorro?.SaldoInicial ?? 0m;
    public decimal FaDepositos => _fondoAhorro?.Depositos ?? 0m;
    public decimal FaRetiros => _fondoAhorro?.Retiros ?? 0m;
    public decimal FaSaldoFinal => _fondoAhorro?.SaldoFinal ?? 0m;

    /// <summary>Formato de pantalla: "29-octubre-2025" (meses en español).</summary>
    private static string FormatoLargo(DateTime d) =>
        Helpers.FormatosFecha.FechaLarga(d);

    partial void OnPestanaActivaChanged(int value)
    {
        OnPropertyChanged(nameof(Pestana0Activa));
        OnPropertyChanged(nameof(Pestana1Activa));
        OnPropertyChanged(nameof(Pestana2Activa));
        OnPropertyChanged(nameof(IconoPestana0));
        OnPropertyChanged(nameof(IconoPestana1));
        OnPropertyChanged(nameof(IconoPestana2));
        OnPropertyChanged(nameof(ColorPestana0));
        OnPropertyChanged(nameof(ColorPestana1));
        OnPropertyChanged(nameof(ColorPestana2));
        OnPropertyChanged(nameof(AvisoFondoVisible));
        OnPropertyChanged(nameof(FabDescargaFaVisible));
        OnPropertyChanged(nameof(FabPdfFaVisible));
        OnPropertyChanged(nameof(FabDescargaEcVisible));
        OnPropertyChanged(nameof(FabPdfEcVisible));
        OnPropertyChanged(nameof(AvisoFacVisible));
        OnPropertyChanged(nameof(FabDescargaFacVisible));
        OnPropertyChanged(nameof(FabPdfFacVisible));
        OnPropertyChanged(nameof(ModalDesdeTexto));
        OnPropertyChanged(nameof(ModalHastaTexto));
        OnPropertyChanged(nameof(ModalPuedeConsultar));
        OnPropertyChanged(nameof(OpacidadConsultar));
        // El aviso rojo del modal compartido bifurca por pestaña.
        NotificarErrorModal();
    }

    partial void OnReporteGeneradoChanged(bool value)
    {
        OnPropertyChanged(nameof(HayReporte));
        OnPropertyChanged(nameof(SinReporte));
        OnPropertyChanged(nameof(SinSorteos));
        OnPropertyChanged(nameof(SorteosEc));
        OnPropertyChanged(nameof(FabDescargaEcVisible));
        foreach (string n in new[]
        {
            nameof(FondoDeAhorro), nameof(Fideicomiso), nameof(Pagares),
            nameof(BolsaElectronica), nameof(GarantiaTotal), nameof(CapacidadDeCredito),
            nameof(VencidoTotal), nameof(ConsignaTotal), nameof(PagosTotal), nameof(TotalFinal),
        })
        {
            OnPropertyChanged(n);
        }
    }

    partial void OnFechaDesdeChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(PuedeConsultar));
        OnPropertyChanged(nameof(ModalPuedeConsultar));
        OnPropertyChanged(nameof(FechaDesdeLarga));
        OnPropertyChanged(nameof(ModalDesdeTexto));
        OnPropertyChanged(nameof(OpacidadConsultar));
    }

    partial void OnFechaHastaChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(PuedeConsultar));
        OnPropertyChanged(nameof(ModalPuedeConsultar));
        OnPropertyChanged(nameof(FechaHastaLarga));
        OnPropertyChanged(nameof(ModalHastaTexto));
        OnPropertyChanged(nameof(OpacidadConsultar));
    }

    partial void OnFondoConsultadoChanged(bool value)
    {
        OnPropertyChanged(nameof(SinFondo));
        OnPropertyChanged(nameof(PeriodoTexto));
        OnPropertyChanged(nameof(FabDescargaFaVisible));
        foreach (string n in new[]
        {
            nameof(FaSaldoInicial), nameof(FaDepositos), nameof(FaRetiros), nameof(FaSaldoFinal),
        })
        {
            OnPropertyChanged(n);
        }
    }

    // ── Estado de Cuenta: comandos ──

    /// <summary>
    /// Genera el reporte: FASE 4 — consulta real GET api/mobile/reportes/
    /// estado-cuenta con overlay "Procesando...." y manejo de errores
    /// (mismo patrón que Fondo de Ahorro): en error el botón queda
    /// reutilizable y un aviso rojo aparece sobre la pestaña.
    /// </summary>
    [RelayCommand]
    private async Task GenerarReporteAsync()
    {
        if (Procesando || (ReporteGenerado && !HayErrorEc))
        {
            return;
        }

        Procesando = true;
        MensajeErrorEc = string.Empty;
        NotificarErrorEc();
        try
        {
            EstadoCuenta? estado = await _servicio.ObtenerAsync();
            if (estado is null)
            {
                MensajeErrorEc = "El reporte no está disponible";
                NotificarErrorEc();
                return;
            }

            EstadoCuenta = estado;
            ReporteGenerado = true;
            OnPropertyChanged(nameof(HayReporte));
            OnPropertyChanged(nameof(SinReporte));
            OnPropertyChanged(nameof(SinSorteos));
            OnPropertyChanged(nameof(SorteosEc));
        }
        catch (TaskCanceledException)
        {
            MensajeErrorEc = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            NotificarErrorEc();
        }
        catch (OperationCanceledException)
        {
            MensajeErrorEc = "Consulta cancelada";
            NotificarErrorEc();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            MensajeErrorEc = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            NotificarErrorEc();
        }
        catch (ApiException ex)
        {
            MensajeErrorEc = ex.Message;
            NotificarErrorEc();
        }
        finally
        {
            Procesando = false;
        }
    }

    /// <summary>
    /// Descarga el Estado de Cuenta: overlay "Descargando...." (~2 s),
    /// genera el PDF real, lo copia a Descargas (MediaStore) y muestra
    /// el FAB PDF.
    /// </summary>
    [RelayCommand]
    private async Task DescargarAsync()
    {
        if (Descargando || EstadoCuenta is null)
        {
            return;
        }

        Descargando = true;
        try
        {
            string ruta = await _pdf.GenerarAsync(EstadoCuenta);
            RutaPdf = ruta;

            await Task.Delay(2000);

#if ANDROID
            string nombre = $"estado-cuenta-{EstadoCuenta.FechaEmision:dd-MM-yyyy}.pdf";
            Platforms.Android.Services.DescargasService.GuardarEnDescargas(ruta, nombre);
#endif
            ReporteDescargado = true;
        }
        finally
        {
            Descargando = false;
        }
    }

    /// <summary>Abre el PDF de Estado de Cuenta con el visor del sistema.</summary>
    [RelayCommand]
    private async Task AbrirPdfAsync()
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
                Title = "Estado de Cuenta",
            });
        }
        catch
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Estado de Cuenta",
                File = new ShareFile(RutaPdf),
            });
        }
    }

    // ── Fondo de Ahorro: comandos ──

    /// <summary>Abre el modal "Seleccionar Fechas".</summary>
    [RelayCommand]
    private void AbrirModalFechas()
    {
        ModalFechasVisible = true;
    }

    /// <summary>Cierra el modal "Seleccionar Fechas" (sin consultar).</summary>
    [RelayCommand]
    private void CerrarModalFechas()
    {
        ModalFechasVisible = false;
    }

    /// <summary>Abre el selector de fecha nativo para el campo Desde (FA o Facturación).</summary>
    [RelayCommand]
    private async Task ElegirDesdeAsync()
    {
        bool esFac = PestanaActiva == 2;
#if ANDROID
        DateTime inicial = esFac ? (FacFechaDesde ?? DateTime.Today) : (FechaDesde ?? DateTime.Today);
        DateTime? elegida = await Platforms.Android.Services.DialogoFechaHoraService.PickFechaAsync(inicial);
#else
        DateTime? elegida = null;
#endif
        if (elegida is not null)
        {
            if (esFac)
            {
                // El TEXTO va antes que la fecha: el label del modal bindea
                // ModalDesdeTexto (computada), que solo se notifica al cambiar
                // la FECHA — si la fecha va primero, notifica con el texto viejo.
                FacDesdeTexto = elegida.Value.ToString("dd/MM/yyyy");
                FacFechaDesde = elegida;
            }
            else
            {
                FechaDesdeTexto = elegida.Value.ToString("dd/MM/yyyy");
                FechaDesde = elegida;
            }
        }
    }

    /// <summary>Abre el selector de fecha nativo para el campo Hasta (FA o Facturación).</summary>
    [RelayCommand]
    private async Task ElegirHastaAsync()
    {
        bool esFac = PestanaActiva == 2;
#if ANDROID
        DateTime inicial = esFac
            ? (FacFechaHasta ?? FacFechaDesde ?? DateTime.Today)
            : (FechaHasta ?? FechaDesde ?? DateTime.Today);
        DateTime? elegida = await Platforms.Android.Services.DialogoFechaHoraService.PickFechaAsync(inicial);
#else
        DateTime? elegida = null;
#endif
        if (elegida is not null)
        {
            if (esFac)
            {
                // Ídem Desde: texto antes que fecha (label del modal al día).
                FacHastaTexto = elegida.Value.ToString("dd/MM/yyyy");
                FacFechaHasta = elegida;
            }
            else
            {
                FechaHastaTexto = elegida.Value.ToString("dd/MM/yyyy");
                FechaHasta = elegida;
            }
        }
    }

    /// <summary>
    /// Consulta el reporte del periodo desde el modal: bifurca por
    /// pestaña (Fondo de Ahorro o Facturación), cierra el modal y
    /// muestra el reporte correspondiente.
    /// </summary>
    [RelayCommand]
    private async Task ConsultarFondoAsync()
    {
        if (PestanaActiva == 2)
        {
            if (FacFechaDesde is null || FacFechaHasta is null || FacFechaDesde.Value > FacFechaHasta.Value)
            {
                return;
            }

            // FASE 3: consulta real al backend con overlay de carga y manejo
            // de errores (mismo patrón que Fondo de Ahorro): el modal NO se
            // cierra hasta que la consulta es exitosa — en error queda
            // abierto con el aviso rojo para reintentar.
            ConsultandoFac = true;
            MensajeErrorFac = string.Empty;
            NotificarErrorModal();
            try
            {
                _facturacion = await _facServicio.ObtenerAsync(FacFechaDesde.Value, FacFechaHasta.Value);
                if (_facturacion is null)
                {
                    MensajeErrorFac = "El reporte no está disponible";
                    NotificarErrorModal();
                    return;
                }

                FacConsultado = true;
                ModalFechasVisible = false;
                // Notificar SIEMPRE: en re-consultas FacConsultado ya era true
                // y OnFacConsultadoChanged no dispara (total/%/periodo quedarían viejos).
                OnPropertyChanged(nameof(FacDesdeLarga));
                OnPropertyChanged(nameof(FacHastaLarga));
                OnPropertyChanged(nameof(FacTotal));
                OnPropertyChanged(nameof(FacPorcentajePrincipal));
                OnPropertyChanged(nameof(FacCategorias));
                OnPropertyChanged(nameof(FacSegmentos));
                PieSolicitaRedibujo?.Invoke(this, EventArgs.Empty);
            }
            catch (TaskCanceledException)
            {
                MensajeErrorFac = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
                NotificarErrorModal();
            }
            catch (OperationCanceledException)
            {
                MensajeErrorFac = "Consulta cancelada";
                NotificarErrorModal();
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                MensajeErrorFac = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
                NotificarErrorModal();
            }
            catch (ApiException ex)
            {
                // 400 (rango inválido) u otros — mensaje del backend.
                MensajeErrorFac = ex.Message;
                NotificarErrorModal();
            }
            finally
            {
                ConsultandoFac = false;
            }
            return;
        }

        if (!PuedeConsultar || FechaDesde!.Value > FechaHasta!.Value)
        {
            return;
        }

        // FASE 2: consulta real al backend con overlay de carga y manejo
        // de errores (patrón DetallePagoViewModel). El modal NO se cierra
        // hasta que la consulta es exitosa — en error queda abierto con
        // el aviso rojo para reintentar.
        ConsultandoFondo = true;
        MensajeErrorFondo = string.Empty;
        OnPropertyChanged(nameof(HayErrorFondo));
        try
        {
            _fondoAhorro = await _fondoServicio.ObtenerAsync(FechaDesde.Value, FechaHasta.Value);
            if (_fondoAhorro is null)
            {
                MensajeErrorFondo = "El reporte no está disponible";
                NotificarErrorModal();
                return;
            }

            FondoConsultado = true;
            ModalFechasVisible = false;
            // Ídem Fondo de Ahorro: el periodo y los saldos deben refrescar
            // en re-consultas (OnFondoConsultadoChanged no dispara si ya
            // era true — mismo bug documentado de Facturación).
            OnPropertyChanged(nameof(FechaDesdeLarga));
            OnPropertyChanged(nameof(FechaHastaLarga));
            foreach (string n in new[]
            {
                nameof(FaSaldoInicial), nameof(FaDepositos),
                nameof(FaRetiros), nameof(FaSaldoFinal),
            })
            {
                OnPropertyChanged(n);
            }

            GraficaFaSolicitaRedibujo?.Invoke(this, EventArgs.Empty);
        }
        catch (TaskCanceledException)
        {
            MensajeErrorFondo = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            NotificarErrorModal();
        }
        catch (OperationCanceledException)
        {
            MensajeErrorFondo = "Consulta cancelada";
            NotificarErrorModal();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            MensajeErrorFondo = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            NotificarErrorModal();
        }
        catch (ApiException ex)
        {
            // 400 (rango inválido) u otros — mensaje del backend.
            MensajeErrorFondo = ex.Message;
            NotificarErrorModal();
        }
        finally
        {
            ConsultandoFondo = false;
        }
    }

    /// <summary>
    /// Descarga el Fondo de Ahorro: overlay "Descargando...." (~2 s),
    /// genera el PDF real, lo copia a Descargas (MediaStore) y muestra
    /// el aviso verde + el FAB PDF.
    /// </summary>
    [RelayCommand]
    private async Task DescargarFondoAsync()
    {
        if (Descargando || _fondoAhorro is null)
        {
            return;
        }

        Descargando = true;
        try
        {
            string ruta = await _fondoPdf.GenerarAsync(_fondoAhorro);
            RutaPdfFondo = ruta;

            await Task.Delay(2000);

#if ANDROID
            string nombre = $"fondo-ahorro-{Helpers.FormatosFecha.FechaArchivo(_fondoAhorro.FechaFin)}.pdf";
            Platforms.Android.Services.DescargasService.GuardarEnDescargas(ruta, nombre);
#endif
            FondoDescargado = true;
            OnPropertyChanged(nameof(FabPdfFaVisible));
        }
        finally
        {
            Descargando = false;
            OnPropertyChanged(nameof(AvisoFondoVisible));
        }
    }

    /// <summary>Abre el PDF de Fondo de Ahorro con el visor del sistema.</summary>
    [RelayCommand]
    private async Task AbrirPdfFondoAsync()
    {
        if (string.IsNullOrEmpty(RutaPdfFondo) || !System.IO.File.Exists(RutaPdfFondo))
        {
            return;
        }

        try
        {
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(RutaPdfFondo),
                Title = "Reporte de Fondo de Ahorro",
            });
        }
        catch
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Reporte de Fondo de Ahorro",
                File = new ShareFile(RutaPdfFondo),
            });
        }
    }

    // ── Facturación: descarga y PDF ──

    /// <summary>
    /// Descarga Facturación: overlay "Descargando...." (~2 s), genera el
    /// PDF real del periodo, lo copia a Descargas (MediaStore) y muestra
    /// el aviso verde + el FAB PDF.
    /// </summary>
    [RelayCommand]
    private async Task DescargarFacAsync()
    {
        if (Descargando || _facturacion is null)
        {
            return;
        }

        Descargando = true;
        try
        {
            string ruta = await _facPdf.GenerarAsync(_facturacion);
            RutaPdfFac = ruta;

            await Task.Delay(2000);

#if ANDROID
            string nombre = $"facturacion-{Helpers.FormatosFecha.FechaArchivo(_facturacion.FechaFin)}.pdf";
            Platforms.Android.Services.DescargasService.GuardarEnDescargas(ruta, nombre);
#endif
            FacDescargado = true;
        }
        finally
        {
            Descargando = false;
        }
    }

    /// <summary>Abre el PDF de Facturación con el visor del sistema.</summary>
    [RelayCommand]
    private async Task AbrirPdfFacAsync()
    {
        if (string.IsNullOrEmpty(RutaPdfFac) || !System.IO.File.Exists(RutaPdfFac))
        {
            return;
        }

        try
        {
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(RutaPdfFac),
                Title = "Reporte de Facturación",
            });
        }
        catch
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Reporte de Facturación",
                File = new ShareFile(RutaPdfFac),
            });
        }
    }

    partial void OnFondoDescargadoChanged(bool value)
    {
        OnPropertyChanged(nameof(FabPdfFaVisible));
        OnPropertyChanged(nameof(AvisoFondoVisible));
    }

    partial void OnFacFechaDesdeChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(ModalDesdeTexto));
        OnPropertyChanged(nameof(ModalPuedeConsultar));
        OnPropertyChanged(nameof(OpacidadConsultar));
        OnPropertyChanged(nameof(FacDesdeLarga));
    }

    partial void OnFacFechaHastaChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(ModalHastaTexto));
        OnPropertyChanged(nameof(ModalPuedeConsultar));
        OnPropertyChanged(nameof(OpacidadConsultar));
        OnPropertyChanged(nameof(FacHastaLarga));
    }

    partial void OnFacConsultadoChanged(bool value)
    {
        OnPropertyChanged(nameof(SinFac));
        OnPropertyChanged(nameof(FacDesdeLarga));
        OnPropertyChanged(nameof(FacHastaLarga));
        OnPropertyChanged(nameof(FacTotal));
        OnPropertyChanged(nameof(FacPorcentajePrincipal));
        OnPropertyChanged(nameof(FacCategorias));
        OnPropertyChanged(nameof(FacSegmentos));
        OnPropertyChanged(nameof(FabDescargaFacVisible));
    }

    partial void OnFacDescargadoChanged(bool value)
    {
        OnPropertyChanged(nameof(FabPdfFacVisible));
        OnPropertyChanged(nameof(AvisoFacVisible));
    }

    partial void OnDescargandoChanged(bool value)
    {
        OnPropertyChanged(nameof(AvisoFondoVisible));
        OnPropertyChanged(nameof(AvisoFacVisible));
    }

    partial void OnReporteDescargadoChanged(bool value)
    {
        OnPropertyChanged(nameof(FabPdfEcVisible));
    }

    /// <summary>Cambia de pestaña desde el tab bar interno.</summary>
    [RelayCommand]
    private void CambiarPestana(string indice)
    {
        if (int.TryParse(indice, out int i) && i >= 0 && i <= 2)
        {
            PestanaActiva = i;
        }
    }

    /// <summary>Regresa al menú de Gestión.</summary>
    [RelayCommand]
    private Task GoBackAsync()
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync("..");
        }
        return Task.CompletedTask;
    }
}
