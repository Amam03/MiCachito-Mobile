using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// VM del formulario de Expendio (mockups 5 y 6): MISMA estructura visual para
/// Actualizar (carga datos del expendio, botón verde "Actualizar") y Crear
/// (campos vacíos, botón verde "Guardar"). Incluye la sección Permisos de
/// Venta, que es la fuente de verdad de qué productos se venden en la pestaña
/// Ventas (spec §9/§14 — no duplicar esa configuración en Ventas).
/// Fase SOLO INTERFAZ: los cambios se aplican en memoria (lista del módulo),
/// SIN endpoints ni persistencia — el contrato del backend no existe aún.
/// </summary>
[QueryProperty(nameof(ExpendioIdStr), "expendioId")]
[QueryProperty(nameof(ModoStr), "modo")]
public partial class ExpendioFormViewModel : BaseViewModel
{
    /// <summary>Expendio en edición (null en modo Crear).</summary>
    private Expendio? _original;

    /// <summary>Copia de trabajo editada por el formulario.</summary>
    private Expendio _edicion = new();

    /// <summary>True en modo Actualizar; False en modo Crear.</summary>
    [ObservableProperty]
    private bool _modoActualizar;

    /// <summary>Título del header ("Actualizar Expendio" / "Crear Expendio").</summary>
    [ObservableProperty]
    private string _titulo = "Crear Expendio";

    /// <summary>Texto del botón verde final ("Actualizar" / "Guardar").</summary>
    [ObservableProperty]
    private string _textoBoton = "Guardar";

    // ── Campos del formulario (bindings de Entry) ──

    [ObservableProperty]
    private string _alias = string.Empty;

    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string _usuario = string.Empty;

    [ObservableProperty]
    private string _contrasena = string.Empty;

    [ObservableProperty]
    private string _domicilio = string.Empty;

    /// <summary>True para ocultar la contraseña (toggle del icono ojo).</summary>
    [ObservableProperty]
    private bool _ocultarContrasena = true;

    // ── Avisos de validación (patrón NuevoDeposito) ──

    [ObservableProperty]
    private bool _avisoAlias;

    [ObservableProperty]
    private bool _avisoNombre;

    [ObservableProperty]
    private bool _avisoUsuario;

    [ObservableProperty]
    private bool _avisoContrasena;

    [ObservableProperty]
    private bool _avisoDomicilio;

    public ExpendioFormViewModel()
    {
        Title = "Expendio";
    }

    /// <summary>Id del expendio recibido vía navegación Shell (solo modo Actualizar).</summary>
    public string? ExpendioIdStr { get; set; }

    /// <summary>Modo recibido vía navegación Shell ("actualizar" / "crear").</summary>
    public string? ModoStr { get; set; }

    /// <summary>
    /// Carga inicial: resuelve el modo, y en Actualizar carga los datos del
    /// expendio elegido sobre la copia de trabajo. Llamado desde OnAppearing
    /// (patrón [QueryProperty] del proyecto: la query llega antes que el
    /// BindingContext aplicado).
    /// </summary>
    public void AlAparecer()
    {
        int id = 0;
        bool actualizar = ModoStr == "actualizar"
            && int.TryParse(ExpendioIdStr, out id);

        if (actualizar)
        {
            _original = ExpendiosDemoData.ObtenerExpendios()
                .FirstOrDefault(e => e.Id == id);
            if (_original is null)
            {
                // Id inexistente: caer a Crear (no crashea).
                actualizar = false;
            }
        }

        ModoActualizar = actualizar;
        Titulo = actualizar ? "Actualizar Expendio" : "Crear Expendio";
        TextoBoton = actualizar ? "Actualizar" : "Guardar";
        Title = Titulo;

        _edicion = _original?.Copia() ?? new Expendio();
        // CRÍTICO: _edicion es una instancia NUEVA; sin esta notificación los
        // bindings compilados (Permisos.X) seguirían apuntando a la instancia
        // anterior y los checkboxes no responderían (bug corregido 2026-09-11).
        OnPropertyChanged(nameof(Permisos));
        Alias = _edicion.Alias;
        Nombre = _edicion.Nombre;
        Usuario = _edicion.Usuario;
        Contrasena = _edicion.Contrasena;
        Domicilio = _edicion.Domicilio;
        OcultarContrasena = true;
    }

    /// <summary>Permisos de venta del expendio en edición (checkboxes del formulario).</summary>
    public PermisosVenta Permisos => _edicion.Permisos;

    /// <summary>True si el botón Actualizar/Guardar está habilitado.</summary>
    public bool PuedeGuardar => true;

    /// <summary>Alterna mostrar/ocultar la contraseña (icono ojo del campo).</summary>
    [RelayCommand]
    private void AlternarContrasena()
    {
        OcultarContrasena = !OcultarContrasena;
    }

    /// <summary>Alterna el permiso Sorteos Tec (checkbox de la fila).</summary>
    [RelayCommand]
    private void AlternarSorteosTec()
    {
        _edicion.Permisos.SorteosTec = !_edicion.Permisos.SorteosTec;
    }

    /// <summary>Alterna el permiso Tiempo Aire (checkbox de la fila).</summary>
    [RelayCommand]
    private void AlternarTiempoAire()
    {
        _edicion.Permisos.TiempoAire = !_edicion.Permisos.TiempoAire;
    }

    /// <summary>Alterna el permiso Lotenal (checkbox de la fila).</summary>
    [RelayCommand]
    private void AlternarLotenal()
    {
        _edicion.Permisos.Lotenal = !_edicion.Permisos.Lotenal;
    }

    /// <summary>Regresa a la lista de expendios sin cambios (flecha del header).</summary>
    [RelayCommand]
    private Task CancelarAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Aplica el formulario: valida los campos (mantiene los valores introducidos),
    /// vuelca la copia de trabajo sobre el expendio (Actualizar) o da de alta uno
    /// nuevo con Id provisional (Crear) y regresa a la lista. En Crear con un
    /// nombre de usuario DIFERENTE al actual se considera un nuevo usuario
    /// (spec §13, escenario B) — la relación queda documentada en docs.
    /// </summary>
    [RelayCommand]
    private async Task GuardarAsync()
    {
        // Validación: marca los campos vacíos y NO limpia lo capturado.
        AvisoAlias = string.IsNullOrWhiteSpace(Alias);
        AvisoNombre = string.IsNullOrWhiteSpace(Nombre);
        AvisoUsuario = string.IsNullOrWhiteSpace(Usuario);
        AvisoContrasena = string.IsNullOrWhiteSpace(Contrasena);
        AvisoDomicilio = string.IsNullOrWhiteSpace(Domicilio);

        if (AvisoAlias || AvisoNombre || AvisoUsuario || AvisoContrasena || AvisoDomicilio)
        {
            return;
        }

        _edicion.Alias = Alias.Trim();
        _edicion.Nombre = Nombre.Trim();
        _edicion.Usuario = Usuario.Trim();
        _edicion.Contrasena = Contrasena;
        _edicion.Domicilio = Domicilio.Trim();

        if (ModoActualizar && _original is not null)
        {
            // Actualizar: mantiene permisos y refleja el cambio en la lista dinámica.
            _original.Alias = _edicion.Alias;
            _original.Nombre = _edicion.Nombre;
            _original.Usuario = _edicion.Usuario;
            _original.Contrasena = _edicion.Contrasena;
            _original.Domicilio = _edicion.Domicilio;
            _original.Permisos.SorteosTec = _edicion.Permisos.SorteosTec;
            _original.Permisos.TiempoAire = _edicion.Permisos.TiempoAire;
            _original.Permisos.Lotenal = _edicion.Permisos.Lotenal;
        }
        else
        {
            // Crear: alta en memoria con Id provisional (backend pendiente).
            // Escenario A: mismo usuario => el usuario queda con N expendios.
            // Escenario B: usuario distinto => nuevo usuario relacionado al creador.
            _edicion.Id = ExpendiosDemoData.SiguienteId();
            ExpendiosDemoData.Agregar(_edicion);
        }

        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
