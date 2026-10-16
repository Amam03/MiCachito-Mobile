using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Contrato de Expendios mobile: consulta del conjunto de expendios del
/// billetero autenticado y actualización de los Permisos de Venta
/// (compartidos a nivel billetero). Los ViewModels nunca hacen HTTP:
/// dependen de esta interfaz (View → ViewModel → Service → ApiClient).
/// </summary>
public interface IExpendiosService
{
    /// <summary>
    /// GET api/mobile/expendios: conjunto de expendios (credenciales) del
    /// billetero de la sesión + estado compartido de permisos.
    /// </summary>
    Task<MobileExpendiosResponse?> ObtenerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// PUT api/mobile/expendios/{idExpendio}/permisos: actualiza los flags
    /// del PROPIO billetero (el idExpendio debe pertenecer al conjunto).
    /// Devuelve el billetero actualizado para refrescar la sesión.
    /// Los parámetros *Original son los valores leídos al abrir el formulario:
    /// el backend los usa para el compare-and-swap y responde 409 si el estado
    /// cambió (p. ej. desde Escritorio) para no sobreescribir datos viejos.
    /// </summary>
    Task<MobilePermisosResponse?> ActualizarPermisosAsync(
        int idExpendio,
        int tieneProdDigitales,
        int tieneTiempoAire,
        int tieneLotenal,
        int prodDigitalesOriginal,
        int tiempoAireOriginal,
        int lotenalOriginal,
        CancellationToken cancellationToken = default);
}
