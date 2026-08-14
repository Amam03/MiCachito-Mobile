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
}
