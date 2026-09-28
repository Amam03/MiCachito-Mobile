using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Carrito de Compras" (pantalla 11), a la
/// que se llega desde el carrito del header de Agregar Boletos (9.x).
///
/// Recibe vía QueryProperty (navegación Shell) el tipo de sorteo
/// (pantalla 6); los registros NO vienen de navegación: se toman del
/// almacén de sesión compartido SesionVentaLotenal (mismas instancias
/// de SerieDisponible que la lista 9.x). Por eso Eliminar/Vender se
/// reflejan en aquella pantalla sin recargar (INotifyPropertyChanged
/// de SerieDisponible) y el badge se refresca al volver (OnAppearing).
///
/// REGISTROS: uno por SERIE con selección (Seleccionadas > 0). El
/// PRECIO por cachito es el real del backend (DotacionDisponible.
/// PrecioFraccion de la dotación en curso) — no hay catálogo de
/// precios local (el precio mostrado proviene del backend, F2 §5).
///
/// Reglas:
///   - Eliminar: quita el registro, descuenta de los totales y la serie
///     vuelve a "0/{total}" (la selección se limpia; el total
///     disponible NO cambia).
///   - Vender (solo estado local hasta F3): cada serie de origen
///     descuenta los cachitos vendidos (TotalDisponible baja), la
///     selección se limpia, el carrito queda vacío y regresa a
///     Agregar Boletos.
///   - Barra inferior: Cantidad = suma de cachitos de todos los
///     registros; Total = suma de importes.
/// </summary>
[QueryProperty(nameof(TipoSorteoIdStr), "tipoSorteoId")]
public partial class CarritoComprasViewModel : BaseViewModel
{
    private readonly MobileVentasService _servicio;

    [ObservableProperty]
    private ObservableCollection<RegistroCarrito> _registros = new();

    /// <summary>True mientras se envía la venta (overlay "Procesando").</summary>
    [ObservableProperty]
    private bool cargando;

    /// <summary>Mensaje de error de la venta (aviso rojo, reintentable).</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    /// <summary>True cuando hay error visible.</summary>
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    private int? _tipoSorteoId;
    private bool _cargado;

    /// <summary>
    /// Id del tipo de sorteo, recibido como string vía navegación Shell
    /// (solo contexto; los registros vienen de la sesión compartida).
    /// </summary>
    public string? TipoSorteoIdStr
    {
        get => _tipoSorteoId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                _tipoSorteoId = id;
                IntentarCargar();
            }
        }
    }

    public CarritoComprasViewModel(MobileVentasService servicio)
    {
        _servicio = servicio;
        Title = "Carrito de Compras";
    }

    // ============ Derivados (notificar con NotificarDerivadas) ============

    /// <summary>Suma de cachitos de todos los registros.</summary>
    public int CantidadBoletos => Registros.Sum(r => r.Cantidad);

    /// <summary>Suma de importes de todos los registros.</summary>
    public decimal TotalImporte => Registros.Sum(r => r.Total);

    /// <summary>Línea "Cantidad" de la barra inferior.</summary>
    public string CantidadTotalTexto => $"Cantidad: {CantidadBoletos}";

    /// <summary>Línea "Total" de la barra inferior.</summary>
    public string TotalTexto => $"Total: ${TotalImporte:0.00}";

    /// <summary>Badge del carrito del header: número de registros.</summary>
    public string BadgeTexto => Registros.Count.ToString();

    /// <summary>True con al menos un registro (oculta el estado vacío).</summary>
    public bool HayRegistros => Registros.Count > 0;

    /// <summary>True sin registros (muestra el mensaje de carrito vacío).</summary>
    public bool SinRegistros => Registros.Count == 0;

    /// <summary>
    /// Carga los registros al recibir el parámetro de navegación. NO
    /// consulta mocks: toma las series con selección del almacén de
    /// sesión SesionVentaLotenal (mismas instancias que la lista 9.x).
    /// </summary>
    private void IntentarCargar()
    {
        if (_cargado || _tipoSorteoId is null)
        {
            return;
        }

        _cargado = true;
        var registros = SesionVentaLotenal.Series
            .Where(s => s.Seleccionadas > 0)
            .Select(CrearRegistro)
            .ToList();

        Registros = new ObservableCollection<RegistroCarrito>(registros);
        NotificarDerivadas();
    }

    /// <summary>
    /// Construye el registro de una serie: sorteo, cantidad, precio
    /// individual y total. El precio por cachito es el REAL del backend
    /// (PrecioFraccion de la dotación en curso); el color, el mapeado
    /// del catálogo de la pantalla 6 (DotacionDisponible.ColorHex).
    /// SorteoTexto: "{producto} {número de dotación}" (ej. "MAYOR 4024").
    /// </summary>
    private RegistroCarrito CrearRegistro(SerieDisponible serie)
    {
        DotacionDisponible? dotacion = SesionVentaLotenal.Dotacion;
        string nombre = dotacion?.NombreProducto ?? string.Empty;
        string numero = dotacion?.NumeroSorteo ?? string.Empty;

        return new RegistroCarrito
        {
            IdSerie = serie.IdSerie,
            SorteoTexto = string.IsNullOrEmpty(numero) ? nombre : $"{nombre} {numero}",
            TiendaTexto = serie.DisplayName,
            Cantidad = serie.Seleccionadas,
            Precio = dotacion?.PrecioFraccion ?? 0m,
            ColorHex = dotacion?.ColorHex ?? "#4125F4"
        };
    }

    /// <summary>
    /// Botón Eliminar de un registro: lo quita del carrito, descuenta
    /// su importe de los totales y la serie vuelve a "0/{total}".
    /// </summary>
    [RelayCommand]
    private void EliminarRegistro(RegistroCarrito? registro)
    {
        if (registro is null)
        {
            return;
        }

        var serie = SesionVentaLotenal.Series
            .FirstOrDefault(s => s.IdSerie == registro.IdSerie);

        if (serie is not null)
        {
            serie.Seleccionadas = 0;
        }

        Registros.Remove(registro);
        NotificarDerivadas();
    }

    /// <summary>
    /// Botón Vender — VENTA REAL (F3): junta los id_billete EXACTOS de
    /// las fracciones libres de cada serie según la cantidad
    /// seleccionada (una fracción = un cachito vendido) y llama a
    /// POST api/mobile/ventas/crear. El backend resuelve vendedor
    /// (sesión), precio (sorteos.precio_fraccion) y totales; marca los
    /// billetes 'vendido' conservando id_billetero_actual.
    ///
    /// Éxito: descuenta localmente (TotalDisponible baja), limpia la
    /// sesión de venta y navega a VentaExitosaLotenal con folio/total
    /// reales. Error (409 ya vendido, 403 ajeno, red): aviso rojo con
    /// el mensaje del backend + botón Reintentar; el carrito se
    /// CONSERVA para reintentar.
    /// </summary>
    [RelayCommand]
    private async Task VenderAsync()
    {
        if (Registros.Count == 0 || IsBusy)
        {
            return;
        }

        // id_billete exactos: las primeras N fracciones libres de cada
        // serie (N = seleccionadas de esa serie).
        var idsBilletes = new List<int>();
        foreach (var registro in Registros)
        {
            var serie = SesionVentaLotenal.Series
                .FirstOrDefault(s => s.IdSerie == registro.IdSerie);

            if (serie is null)
            {
                continue;
            }

            idsBilletes.AddRange(serie.FraccionesLibres
                .Take(serie.Seleccionadas)
                .Select(f => f.IdBillete));
        }

        if (idsBilletes.Count == 0)
        {
            MensajeError = "Los boletos seleccionados ya no están disponibles. Vuelve a entrar a la dotación para actualizar.";
            OnPropertyChanged(nameof(HayError));
            return;
        }

        IsBusy = true;
        Cargando = true;
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(HayError));

        try
        {
            VentaCreadaApi venta = await _servicio.CrearVentaAsync(idsBilletes).ConfigureAwait(true);

            // Éxito: descuento local + limpieza + navegación con datos reales.
            foreach (var registro in Registros.ToList())
            {
                var serie = SesionVentaLotenal.Series
                    .FirstOrDefault(s => s.IdSerie == registro.IdSerie);

                if (serie is not null)
                {
                    serie.TotalDisponible = Math.Max(0, serie.TotalDisponible - registro.Cantidad);
                    serie.Seleccionadas = 0;
                }
            }

            Registros.Clear();
            NotificarDerivadas();

            string boletos = idsBilletes.Count == 1
                ? "1 boleto"
                : $"{idsBilletes.Count} boletos";

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(Views.VentaExitosaLotenalPage)}" +
                    $"?folio={Uri.EscapeDataString(venta.Folio)}" +
                    $"&total={Uri.EscapeDataString($"${venta.Total:0.00}")}" +
                    $"&boletos={Uri.EscapeDataString(boletos)}");
            }
        }
        catch (TaskCanceledException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (OperationCanceledException)
        {
            MensajeError = "Venta cancelada";
            OnPropertyChanged(nameof(HayError));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (ApiException ex)
        {
            MensajeError = ex.Message;
            OnPropertyChanged(nameof(HayError));
        }
        finally
        {
            Cargando = false;
            IsBusy = false;
        }
    }

    /// <summary>
    /// Retrocede a la pantalla anterior (Agregar Boletos).
    /// </summary>
    [RelayCommand]
    private Task GoBackAsync()
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync("..");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Notifica a la UI las propiedades derivadas de la barra inferior
    /// y del badge (llamar tras cualquier cambio de Registros).
    /// </summary>
    private void NotificarDerivadas()
    {
        OnPropertyChanged(nameof(CantidadBoletos));
        OnPropertyChanged(nameof(TotalImporte));
        OnPropertyChanged(nameof(CantidadTotalTexto));
        OnPropertyChanged(nameof(TotalTexto));
        OnPropertyChanged(nameof(BadgeTexto));
        OnPropertyChanged(nameof(HayRegistros));
        OnPropertyChanged(nameof(SinRegistros));
    }
}
