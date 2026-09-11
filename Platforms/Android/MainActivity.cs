using Android.App;
using Android.Content.PM;
using Android.OS;

namespace MiCachito.Mobile
{
    /// <summary>
    /// Activity principal. Intercepta el botón Atrás para evitar el
    /// crash de plataforma de MAUI 10 (dotnet/maui#32750): al destruirse
    /// la Activity con fragments del Shell activos, OnDestroy de
    /// ShellFragmentContainer accede a un IServiceProvider ya liberado
    /// y la app muere. En su lugar, el back en pantalla raíz manda la
    /// app a segundo plano (comportamiento estándar de apps Android).
    /// </summary>
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        /// <summary>
        /// Back en la raíz (Login tras cerrar sesión, o tab del Shell sin
        /// pila de navegación): manda la app a segundo plano en vez de
        /// finalizar la Activity (evita el crash del provider disposed).
        /// Si MAUI aún tiene a dónde volver (página empujada), se deja
        /// pasar el back para que MAUI haga el pop normal.
        ///
        /// Obsoleto desde API 33 (back predictivo): SE USA A PROPÓSITO —
        /// el manifest desactiva enableOnBackInvokedCallback, así el
        /// sistema invoca este override clásico.
        /// </summary>
#pragma warning disable CS0612 // OnBackPressed obsoleto: intencional con opt-out del back predictivo
        public override void OnBackPressed()
#pragma warning restore CS0612
        {
            var app = Microsoft.Maui.Controls.Application.Current;
            var page = app is not null && app.Windows.Count > 0
                ? app.Windows[0].Page
                : null;

            if (page is Microsoft.Maui.Controls.Shell shell
                && shell.Navigation.NavigationStack.Count > 1)
            {
                // Hay páginas empujadas (Notificaciones, Dispositivos...):
                // MAUI hace el pop normal.
                base.OnBackPressed();
            }
            else
            {
                // Raíz: a segundo plano (moveTaskToBack), NO finish().
                MoveTaskToBack(true);
            }
        }
    }
}
