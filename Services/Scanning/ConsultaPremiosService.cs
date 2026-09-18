using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services.Scanning;

/// <summary>
/// Resultado de la consulta de premios para un boleto escaneado.
/// </summary>
public enum ResultadoPremio
{
    TienePremio,
    NoTienePremio,
    Reintegro,
}

/// <summary>
/// Consulta de premios REAL contra POST /api/mobile/premios/consultar.
///
/// El backend resuelve el sorteo (fecha ± subcódigo de edición), busca en
/// premios (poblada por la sábana de premios y reintegros cargada desde
/// Escritorio) con fallbacks de serie ('00' tradicional) y fracción (0 =
/// nivel billete), y devuelve montos por cachito (total/vigésimos).
///
/// El servicio NO interpreta la respuesta: devuelve la RespuestaPremioApi
/// tal cual y deja la interpretación (estados de color/mensaje) al VM.
/// Lanza ApiException en errores HTTP/2xx-con-success=false; la red caída
/// llega como HttpRequestException/TaskCanceled — el VM la captura y
/// muestra "sin conexión" SIN inventar un estado falso.
/// </summary>
public sealed class ConsultaPremiosService
{
    private const string RutaConsultar = "api/mobile/premios/consultar";

    private readonly IApiClient _api;

    public ConsultaPremiosService(IApiClient api)
    {
        _api = api;
    }

    /// <summary>
    /// Consulta el resultado de un boleto ya parseado. Payload con los
    /// campos que salen del QR: billete, serie (o signo zodiacal como
    /// serie), fracción, fecha del sorteo y subcódigo de edición.
    /// </summary>
    public async Task<RespuestaPremioApi> ConsultarAsync(BilleteParseado parseado, CancellationToken cancellationToken = default)
    {
        string serie = parseado.EsZodiaco
            ? (parseado.SignoCodigo ?? string.Empty)
            : (parseado.Serie ?? string.Empty);

        var payload = new
        {
            numero_billete = parseado.NumeroBillete,
            serie,
            fraccion = parseado.Fraccion,
            fecha_sorteo = parseado.FechaSorteo,
            subcodigo_sorteo = parseado.SubCodigo,
            es_zodiaco = parseado.EsZodiaco,
        };

        RespuestaPremioApi? respuesta = await _api.PostAsync<RespuestaPremioApi>(RutaConsultar, payload, cancellationToken)
            .ConfigureAwait(false);

        return respuesta ?? new RespuestaPremioApi
        {
            Boleto = parseado.NumeroBillete ?? string.Empty,
            Serie = serie,
            Resultado = "NO GANADOR",
            Motivo = "SIN_RESPUESTA",
        };
    }
}
