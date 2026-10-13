using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Depósitos del billetero contra la API real (/api/mobile/depositos/*):
/// historial (GET), registro (POST con folio servidor DEP-YYYYMMDD-NNNN y
/// estatus 'pendiente' hasta que Caja lo valide desde Escritorio) y
/// catálogo de cuentas bancarias activas para el selector (mismo catálogo
/// que api/cuentas-bancarias de Desktop).
///
/// El id_billetero NUNCA se envía: el backend lo toma de la sesión.
/// Lanza ApiException en errores HTTP; la red caída llega como
/// HttpRequestException/TaskCanceled y los VMs la capturan.
/// </summary>
public class DepositoService
{
    private const string RutaDepositos = "api/mobile/depositos";
    private const string RutaCuentas = "api/mobile/depositos/cuentas";

    private readonly IApiClient _api;

    public DepositoService(IApiClient api)
    {
        _api = api;
    }

    /// <summary>Historial de depósitos del billetero (más reciente primero).</summary>
    public async Task<List<DepositoApi>> CargarDepositosAsync(CancellationToken cancellationToken = default)
    {
        List<DepositoApi>? items = await _api.GetAsync<List<DepositoApi>>(RutaDepositos, cancellationToken)
            .ConfigureAwait(false);
        return items ?? new List<DepositoApi>();
    }

    /// <summary>
    /// Registra un depósito del billetero de la sesión. El folio y el
    /// estatus ('pendiente') los asigna el backend.
    /// </summary>
    /// <param name="idCuentaBancaria">Cuenta activa del selector.</param>
    /// <param name="fechaDeposito">"yyyy-MM-dd".</param>
    /// <param name="horaDeposito">"HH:mm" o null.</param>
    /// <param name="monto">Monto mayor a 0.</param>
    /// <param name="referencia">Folio/Movimiento del formulario.</param>
    /// <param name="observaciones">Nombre del sorteo asociado.</param>
    public async Task<DepositoCreadoApi> RegistrarDepositoAsync(
        int idCuentaBancaria,
        string fechaDeposito,
        string? horaDeposito,
        decimal monto,
        string? referencia,
        string? observaciones,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            id_cuenta_bancaria = idCuentaBancaria,
            fecha_deposito = fechaDeposito,
            hora_deposito = horaDeposito,
            monto = monto,
            referencia = referencia,
            observaciones = observaciones,
        };

        DepositoCreadoApi? creado = await _api.PostAsync<DepositoCreadoApi>(RutaDepositos, body, cancellationToken)
            .ConfigureAwait(false);

        return creado ?? throw new ApiException(
            System.Net.HttpStatusCode.OK, "El servidor no devolvió el depósito registrado");
    }

    /// <summary>Cuentas bancarias activas para el selector de banco.</summary>
    public async Task<List<CuentaBancariaApi>> CargarCuentasAsync(CancellationToken cancellationToken = default)
    {
        List<CuentaBancariaApi>? items = await _api.GetAsync<List<CuentaBancariaApi>>(RutaCuentas, cancellationToken)
            .ConfigureAwait(false);
        return items ?? new List<CuentaBancariaApi>();
    }
}
