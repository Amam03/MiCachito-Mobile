using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// VM de Expendios (mockups Expendios 1-4): pestaña 1 = Consulta de registros
/// (periodo + modal fechas/expendios + estado Sin Registros), pestaña 2 =
/// Administración de expendios (tarjetas + FAB crear). El encabezado replica
/// el de Gestión (saldo global).
/// Fase SOLO INTERFAZ: sin registros en la consulta (directriz del spec; los
/// resultados reales llegarán con el backend).
/// </summary>
public partial class ExpendiosViewModel : BaseViewModel
{
    /// <summary>Saldo global del encabezado (mismo valor que Gestión: "$0.00").</summary>
    [ObservableProperty]
    private string _saldo = "$0.00";

    /// <summary>Pestaña activa (0=Consulta de registros, 1=Administración de expendios).</summary>
    [ObservableProperty]
    private int _pestanaActiva;

    /// <summary>True mientras corre "Procesando..." tras presionar Consultar.</summary>
    [ObservableProperty]
    private bool _procesando;

    /// <summary>True cuando el modal "Seleccionar Fechas" está abierto.</summary>
    [ObservableProperty]
    private bool _modalFechasVisible;

    /// <summary>Fecha Desde de la consulta (por defecto: fecha actual, no fija).</summary>
    [ObservableProperty]
    private DateTime _fechaDesde = DateTime.Today;

    /// <summary>Fecha Hasta de la consulta (por defecto: fecha actual, no fija).</summary>
    [ObservableProperty]
    private DateTime _fechaHasta = DateTime.Today;

    /// <summary>Texto del campo Desde del modal (dd/MM/yyyy).</summary>
    [ObservableProperty]
    private string _desdeTexto = string.Empty;

    /// <summary>Texto del campo Hasta del modal (dd/MM/yyyy).</summary>
    [ObservableProperty]
    private string _hastaTexto = string.Empty;

    /// <summary>
    /// Expendios del usuario actual (lista dinámica: 1 o N según su relación).
    /// Mantiene las MISMAS instancias del catálogo compartido; tras crear uno
    /// nuevo desde el formulario, <see cref="AlAparecer"/> re-sincroniza.
    /// </summary>
    public ObservableCollection<Expendio> Expendios { get; } = [];

    public ExpendiosViewModel()
    {
        Title = "Expendios";
        SincronizarExpendios();
        DesdeTexto = FechaDesde.ToString("dd/MM/yyyy");
        HastaTexto = FechaHasta.ToString("dd/MM/yyyy");
    }

    /// <summary>
    /// Re-sincroniza la colección observable con el catálogo compartido (mantiene
    /// las instancias existentes, agrega las nuevas y quita las eliminadas).
    /// Llamado al aparecer la página (patrón AlAparecer del proyecto) para que
    /// la lista refleje lo creado/actualizado en el formulario.
    /// </summary>
    public void AlAparecer()
    {
        SincronizarExpendios();
        OnPropertyChanged(nameof(PuedeConsultar));
        OnPropertyChanged(nameof(OpacidadConsultar));
    }

    private void SincronizarExpendios()
    {
        foreach (Expendio e in ExpendiosDemoData.ObtenerExpendios())
        {
            if (Expendios.All(x => x.Id != e.Id))
            {
                Expendios.Add(e);
                e.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(Expendio.SeleccionadoConsulta))
                    {
                        OnPropertyChanged(nameof(PuedeConsultar));
                        OnPropertyChanged(nameof(OpacidadConsultar));
                    }
                };
            }
        }
    }

    // ── Derivadas ──

    /// <summary>True si la pestaña Consulta de registros está activa.</summary>
    public bool Pestana0Activa => PestanaActiva == 0;

    /// <summary>True si la pestaña Administración de expendios está activa.</summary>
    public bool Pestana1Activa => PestanaActiva == 1;

    /// <summary>Icono de la pestaña 1 (lista; blanca la activa, indigo tenue la inactiva).</summary>
    public string IconoPestana0 => Pestana0Activa ? "icon_lista_blanco.svg" : "icon_lista_indigo.svg";

    /// <summary>Icono de la pestaña 2 (tienda; blanca la activa, indigo tenue la inactiva).</summary>
    public string IconoPestana1 => Pestana1Activa ? "icon_tienda_blanco.svg" : "icon_tienda_indigo.svg";

    /// <summary>Periodo formateado "yyyy-MM-dd - yyyy-MM-dd" (formato de los mockups 1/3).</summary>
    public string PeriodoTexto => $"{FechaDesde:yyyy-MM-dd} - {FechaHasta:yyyy-MM-dd}";

    /// <summary>True si hay al menos un expendio marcado en el modal (habilita Consultar).</summary>
    public bool PuedeConsultar => Expendios.Any(e => e.SeleccionadoConsulta);

    /// <summary>Opacidad del botón Consultar (0.5 deshabilitado, 1 habilitado).</summary>
    public float OpacidadConsultar => PuedeConsultar ? 1f : 0.5f;

    partial void OnPestanaActivaChanged(int value)
    {
        OnPropertyChanged(nameof(Pestana0Activa));
        OnPropertyChanged(nameof(Pestana1Activa));
        OnPropertyChanged(nameof(IconoPestana0));
        OnPropertyChanged(nameof(IconoPestana1));
    }

    partial void OnFechaDesdeChanged(DateTime value)
    {
        OnPropertyChanged(nameof(PeriodoTexto));
    }

    partial void OnFechaHastaChanged(DateTime value)
    {
        OnPropertyChanged(nameof(PeriodoTexto));
    }

    // ── Comandos ──

    /// <summary>Cambia entre las dos pestañas del módulo (estado fluido, sin recargar).</summary>
    [RelayCommand]
    private void CambiarPestana(string indice)
    {
        if (int.TryParse(indice, out int i) && i is 0 or 1)
        {
            PestanaActiva = i;
        }
    }

    /// <summary>Abre el modal "Seleccionar Fechas" (botón calendario).</summary>
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

    /// <summary>Alterna la marca de un expendio en el modal de consulta.</summary>
    [RelayCommand]
    private void AlternarExpendio(Expendio expendio)
    {
        expendio.SeleccionadoConsulta = !expendio.SeleccionadoConsulta;
    }

    /// <summary>Abre el selector de fecha nativo para el campo Desde.</summary>
    [RelayCommand]
    private async Task ElegirDesdeAsync()
    {
#if ANDROID
        DateTime? elegida = await Platforms.Android.Services.DialogoFechaHoraService
            .PickFechaAsync(FechaDesde);
#else
        DateTime? elegida = null;
#endif
        if (elegida is not null)
        {
            FechaDesde = elegida.Value;
            DesdeTexto = elegida.Value.ToString("dd/MM/yyyy");
        }
    }

    /// <summary>Abre el selector de fecha nativo para el campo Hasta.</summary>
    [RelayCommand]
    private async Task ElegirHastaAsync()
    {
#if ANDROID
        DateTime? elegida = await Platforms.Android.Services.DialogoFechaHoraService
            .PickFechaAsync(FechaHasta);
#else
        DateTime? elegida = null;
#endif
        if (elegida is not null)
        {
            FechaHasta = elegida.Value;
            HastaTexto = elegida.Value.ToString("dd/MM/yyyy");
        }
    }

    /// <summary>
    /// Consulta de registros: valida el rango (Desde ≤ Hasta), considera los
    /// expendios marcados y muestra "Procesando..." (~1.5 s). Fase actual:
    /// SIN registros (estado vacío "Sin Registros" hasta que exista contrato
    /// del backend y capturas de referencia).
    /// </summary>
    [RelayCommand]
    private async Task ConsultarAsync()
    {
        if (Procesando)
        {
            return;
        }

        if (FechaDesde > FechaHasta)
        {
            if (Application.Current is not null && Application.Current.Windows.Count > 0)
            {
                await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                    "Consulta", "La fecha Desde no puede ser mayor que la fecha Hasta.", "OK");
            }
            return;
        }

        Procesando = true;
        ModalFechasVisible = false;
        try
        {
            // Estructura para la consulta real (backend pendiente): rango + expendios marcados.
            List<Expendio> seleccionados = Expendios.Where(e => e.SeleccionadoConsulta).ToList();
            await Task.Delay(1500);
            // Resultado de la fase UI: estado vacío "Sin Registros" (mockup 3).
        }
        finally
        {
            Procesando = false;
        }
    }

    /// <summary>
    /// Impresión del resultado filtrado (spec §6): el icono existe y queda
    /// estructurado, PERO el formato de impresión real está PENDIENTE de
    /// capturas de referencia — no se implementa un formato inventado.
    /// </summary>
    [RelayCommand]
    private async Task ImprimirAsync()
    {
        if (Application.Current is not null && Application.Current.Windows.Count > 0)
        {
            await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                "Impresión", "Funcionalidad de impresión pendiente de definición.", "OK");
        }
    }

    /// <summary>Navega a Actualizar Expendio con los datos del expendio elegido.</summary>
    [RelayCommand]
    private Task EditarAsync(Expendio expendio)
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(
            $"{nameof(Views.ExpendioFormPage)}?expendioId={expendio.Id}&modo=actualizar");
    }

    /// <summary>Navega a Crear Expendio (botón flotante +).</summary>
    [RelayCommand]
    private Task CrearAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync($"{nameof(Views.ExpendioFormPage)}?modo=crear");
    }
}
