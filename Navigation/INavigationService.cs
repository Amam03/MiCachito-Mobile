namespace MiCachito.Mobile.Navigation;

/// <summary>
/// Servicio de navegación que abstrae el Shell de .NET MAUI.
/// Los ViewModels solo dependen de esta interfaz, nunca de Shell/Page concretas.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navega al Home (AppShell/HomePage). Reemplaza la raíz actual si es necesario.
    /// </summary>
    Task NavigateToHomeAsync();

    /// <summary>
    /// Navega al Login. Reemplaza la raíz actual.
    /// </summary>
    Task NavigateToLoginAsync();

    /// <summary>
    /// Navega a la pantalla de Tiempo Aire dentro del Shell actual.
    /// </summary>
    Task NavigateToTiempoAireAsync();

    /// <summary>
    /// Navega a la pantalla de montos del proveedor de Tiempo Aire seleccionado.
    /// </summary>
    /// <param name="proveedorId">Id del proveedor (ver TiempoAireData).</param>
    Task NavigateToMontosTiempoAireAsync(int proveedorId);

    /// <summary>
    /// Navega a la pantalla de número telefónico pasando el proveedor y monto.
    /// </summary>
    Task NavigateToNumeroTelefonoTiempoAireAsync(int proveedorId, decimal monto);

    /// <summary>
    /// Navega a la pantalla de sorteos de Lotería Nacional (Sr.B. LOTENAL).
    /// </summary>
    Task NavigateToSorteosLotenalAsync();

    /// <summary>
    /// Navega a la pantalla de sorteos activos del tipo seleccionado.
    /// </summary>
    /// <param name="tipoSorteoId">Id del tipo de sorteo (ver SorteosLotenalData).</param>
    Task NavigateToSorteosActivosAsync(int tipoSorteoId);

    /// <summary>
    /// Navega a la pantalla "Seleccionar Ciudad" tras elegir un sorteo activo.
    /// </summary>
    /// <param name="sorteoId">Id del sorteo activo seleccionado (ver SorteosActivosLotenalData).</param>
    /// <param name="tipoSorteoId">Id del tipo de sorteo (contexto de navegación).</param>
    Task NavigateToSeleccionCiudadAsync(int sorteoId, int tipoSorteoId);

    /// <summary>
    /// Navega a la pantalla "Agregar Boletos" (pantallas 9.1 / 9.2) tras
    /// seleccionar la ciudad, pasando el contexto completo del flujo.
    /// AgregarBoletosViewModel los recibe vía [QueryProperty("sorteoId")],
    /// [QueryProperty("tipoSorteoId")] y [QueryProperty("ciudadId")].
    /// </summary>
    /// <param name="sorteoId">Id del sorteo activo seleccionado (pantalla 7.x).</param>
    /// <param name="tipoSorteoId">Id del tipo de sorteo (contexto de navegación).</param>
    /// <param name="ciudadId">Id de la ciudad seleccionada (0 = "Cualquier ciudad").</param>
    Task NavigateToAgregarBoletosAsync(int sorteoId, int tipoSorteoId, int ciudadId);
}
