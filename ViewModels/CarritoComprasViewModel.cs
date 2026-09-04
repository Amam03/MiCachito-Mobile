using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Carrito de Compras" (pantalla 11), a la que
/// se llega desde el carrito del header de Agregar Boletos.
///
/// Recibe vía QueryProperty (navegación Shell):
///   - sorteoId: sorteo activo en curso (pantalla 7.x)
///   - tipoSorteoId: tipo de sorteo (pantalla 6)
///
/// REGISTROS: uno por tienda con selección (Seleccionadas > 0). Se
/// reconstruyen al ENTRAR desde el catálogo compartido (las mismas
/// instancias que la lista 9.x): por eso Eliminar/Vender se reflejan
/// en aquella pantalla sin recargarla (INotifyPropertyChanged de
/// TiendaDisponible) y el badge se refresca al volver (OnAppearing).
///
/// Reglas:
///   - Eliminar: quita el registro, descuenta de los totales y la tienda
///     vuelve a "0/{total}" (la selección se limpia; el total disponible
///     NO cambia).
///   - Vender (solo estado local, sin backend): cada tienda de origen
///     descuenta los boletos vendidos (total disponible baja), la
///     selección se limpia, el carrito queda vacío y regresa a
///     Agregar Boletos. "Los boletos pasan al inventario de la
///     sucursal" aún no tiene representación visual (futuro backend).
///   - Barra inferior: Cantidad = suma de boletos de todos los
///     registros; Total = suma de importes.
/// </summary>
[QueryProperty(nameof(SorteoIdStr), "sorteoId")]
[QueryProperty(nameof(TipoSorteoIdStr), "tipoSorteoId")]
public partial class CarritoComprasViewModel : BaseViewModel
{
    /// <summary>Ids de los tipos de sorteo zodiacales (ver SorteosLotenalData).</summary>
    private const int TipoZodiaco = 3;
    private const int TipoZodiacoEspecial = 4;

    [ObservableProperty]
    private ObservableCollection<RegistroCarrito> _registros = new();

    private int? _sorteoId;
    private int? _tipoSorteoId;
    private bool _cargado;
    private SorteoActivoLotenal? _sorteo;

    /// <summary>
    /// Id del sorteo activo, recibido como string vía navegación Shell.
    /// </summary>
    public string? SorteoIdStr
    {
        get => _sorteoId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                _sorteoId = id;
                IntentarCargar();
            }
        }
    }

    /// <summary>
    /// Id del tipo de sorteo, recibido como string vía navegación Shell.
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

    public CarritoComprasViewModel()
    {
        Title = "Carrito de Compras";
    }

    private bool EsZodiaco => _tipoSorteoId is TipoZodiaco or TipoZodiacoEspecial;

    // ============ Derivados (notificar con NotificarDerivadas) ============

    /// <summary>Suma de boletos de todos los registros.</summary>
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
    /// Carga los registros solo cuando los dos parámetros de navegación
    /// están disponibles (Shell los asigna en orden indeterminado).
    /// </summary>
    private void IntentarCargar()
    {
        if (_cargado || _sorteoId is null || _tipoSorteoId is null)
        {
            return;
        }

        _cargado = true;
        _sorteo = SorteosActivosLotenalData.ObtenerPorTipoId(_tipoSorteoId.Value)
            .FirstOrDefault(s => s.IdSorteo == _sorteoId.Value);

        if (_sorteo is null)
        {
            return;
        }

        var registros = TiendasDisponiblesData.ObtenerPorCiudad(0, EsZodiaco)
            .Where(t => t.Seleccionadas > 0)
            .Select(CrearRegistro)
            .ToList();

        Registros = new ObservableCollection<RegistroCarrito>(registros);
        NotificarDerivadas();
    }

    /// <summary>
    /// Construye el registro de una tienda: sorteo, cantidad, precio
    /// individual y total. El precio por cachito es el PRECIO DEL
    /// SORTEO tomado de la fuente existente (SorteosActivosLotenalData,
    /// alineada al backend): MAYOR $30, SUPERIOR $40, ZODIACO $20,
    /// ZODIACO ESPECIAL $35, ESPECIAL $60, GRAN ESPECIAL $250,
    /// MAGNO $120 y GORDITO NAVIDEÑO $120. No se duplica la fuente
    /// ni se inventa otra: se toma del catálogo ya cargado (_sorteo).
    /// </summary>
    private RegistroCarrito CrearRegistro(TiendaDisponible tienda)
    {
        return new RegistroCarrito
        {
            IdTienda = tienda.IdTienda,
            SorteoTexto = $"{NombreCortoSorteo()} {NumeroDeSorteo()}",
            TiendaTexto = tienda.DisplayName,
            Cantidad = tienda.Seleccionadas,
            Precio = _sorteo!.Precio,
            ColorHex = _sorteo!.ColorHex
        };
    }

    private string NumeroDeSorteo() => _sorteo?.NumeroSorteo ?? string.Empty;

    /// <summary>
    /// Nombre corto del sorteo para la tarjeta (ej. "SUPERIOR",
    /// "GRAN ESPECIAL", "GORDITO NAVIDEÑO"): NombreSorteo sin el
    /// prefijo "SORTEO ". Evita etiquetas ambiguas como
    /// "ESPECIAL 208" para el Gran Especial.
    /// </summary>
    private string NombreCortoSorteo()
    {
        const string prefijo = "SORTEO ";

        return _sorteo?.NombreSorteo.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase) == true
            ? _sorteo.NombreSorteo[prefijo.Length..]
            : _sorteo?.NombreSorteo ?? string.Empty;
    }

    /// <summary>
    /// Botón Eliminar de un registro: lo quita del carrito, descuenta
    /// su importe de los totales y la tienda vuelve a "0/{total}".
    /// </summary>
    [RelayCommand]
    private void EliminarRegistro(RegistroCarrito? registro)
    {
        if (registro is null)
        {
            return;
        }

        var tienda = TiendasDisponiblesData.ObtenerPorCiudad(0, EsZodiaco)
            .FirstOrDefault(t => t.IdTienda == registro.IdTienda);

        if (tienda is not null)
        {
            tienda.Seleccionadas = 0;
        }

        Registros.Remove(registro);
        NotificarDerivadas();
    }

    /// <summary>
    /// Botón Vender: por ahora SOLO estado local (sin backend). Los
    /// boletos se descuentan del origen (el total disponible de cada
    /// tienda baja), las selecciones se limpian, el carrito queda
    /// vacío y regresa a Agregar Boletos. El paso "a inventario de la
    /// sucursal" no tiene representación visual todavía.
    /// </summary>
    [RelayCommand]
    private Task VenderAsync()
    {
        if (Registros.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (var registro in Registros)
        {
            var tienda = TiendasDisponiblesData.ObtenerPorCiudad(0, EsZodiaco)
                .FirstOrDefault(t => t.IdTienda == registro.IdTienda);

            if (tienda is not null)
            {
                tienda.TotalDisponible = Math.Max(0, tienda.TotalDisponible - registro.Cantidad);
                tienda.Seleccionadas = 0;
            }
        }

        Registros.Clear();
        NotificarDerivadas();

        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync("..");
        }

        return Task.CompletedTask;
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
