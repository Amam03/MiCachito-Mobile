using CommunityToolkit.Mvvm.ComponentModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Expendio asociado al usuario actual (mockups Expendios 1-6, fase SOLO INTERFAZ).
/// Un usuario puede tener uno o varios expendios (relación 1-N); el alias es el
/// nombre corto con el que se identifica y el titular es el nombre completo.
/// Los datos reales llegarán desde el backend (aún sin contrato definido).
/// </summary>
public partial class Expendio : ObservableObject
{
    /// <summary>Identificador del expendio (del backend cuando se integre).</summary>
    public int Id { get; set; }

    /// <summary>Alias corto del expendio (ej. "Centro").</summary>
    [ObservableProperty]
    private string _alias = string.Empty;

    /// <summary>Nombre del titular (ej. "DEMETRIO HECTOR CELIS VELEZ").</summary>
    [ObservableProperty]
    private string _nombre = string.Empty;

    /// <summary>Nombre de usuario asociado al expendio (ej. "juan").</summary>
    [ObservableProperty]
    private string _usuario = string.Empty;

    /// <summary>Contraseña del usuario del expendio (protegida en UI).</summary>
    [ObservableProperty]
    private string _contrasena = string.Empty;

    /// <summary>Domicilio del expendio (ej. "CALLE 214 C, SAN JERONIMO").</summary>
    [ObservableProperty]
    private string _domicilio = string.Empty;

    /// <summary>
    /// Marcado en el modal "Seleccionar Fechas y Expendios" (consulta de
    /// registros). Estado de UI sobre la instancia compartida del catálogo,
    /// patrón TiendaDisponible.
    /// </summary>
    [ObservableProperty]
    private bool _seleccionadoConsulta = true;

    /// <summary>Negación de <see cref="SeleccionadoConsulta"/> (checkbox vacío, sin converters).</summary>
    public bool NoSeleccionadoConsulta => !SeleccionadoConsulta;

    partial void OnSeleccionadoConsultaChanged(bool value)
    {
        OnPropertyChanged(nameof(NoSeleccionadoConsulta));
    }

    /// <summary>Permisos de venta del expendio (fuente de verdad para la pestaña Ventas).</summary>
    public PermisosVenta Permisos { get; set; } = new();

    /// <summary>
    /// Copia de trabajo para el formulario de Actualizar/Crear: permite editar
    /// valores sin mutar el expendio real hasta presionar Actualizar/Guardar.
    /// </summary>
    public Expendio Copia()
    {
        return new Expendio
        {
            Id = Id,
            Alias = Alias,
            Nombre = Nombre,
            Usuario = Usuario,
            Contrasena = Contrasena,
            Domicilio = Domicilio,
            Permisos = new PermisosVenta
            {
                SorteosTec = Permisos.SorteosTec,
                TiempoAire = Permisos.TiempoAire,
                Lotenal = Permisos.Lotenal,
            },
        };
    }
}

/// <summary>
/// Permisos de venta de un expendio (sección del formulario Actualizar/Crear).
/// Regla de negocio (spec Expendios §9): las opciones marcadas determinan qué
/// productos estarán disponibles en la pestaña Ventas — la pestaña Ventas NO
/// debe duplicar esta configuración; la fuente de verdad es el expendio.
/// </summary>
public partial class PermisosVenta : ObservableObject
{
    /// <summary>Permiso para vender Sorteos Tec.</summary>
    [ObservableProperty]
    private bool _sorteosTec;

    /// <summary>Permiso para vender Tiempo Aire.</summary>
    [ObservableProperty]
    private bool _tiempoAire;

    /// <summary>Permiso para vender Lotenal.</summary>
    [ObservableProperty]
    private bool _lotenal;

    /// <summary>Negaciones (checkboxes vacíos, sin converters).</summary>
    public bool NoSorteosTec => !SorteosTec;
    public bool NoTiempoAire => !TiempoAire;
    public bool NoLotenal => !Lotenal;

    partial void OnSorteosTecChanged(bool value) => OnPropertyChanged(nameof(NoSorteosTec));
    partial void OnTiempoAireChanged(bool value) => OnPropertyChanged(nameof(NoTiempoAire));
    partial void OnLotenalChanged(bool value) => OnPropertyChanged(nameof(NoLotenal));
}
