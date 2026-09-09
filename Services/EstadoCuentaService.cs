using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Fuente del reporte Estado de Cuenta (pestaña 1 de Reportes).
/// Fase SOLO INTERFAZ: devuelve la estructura en cero y SIN sorteos
/// (directriz del usuario: ni mocks ni seeds; los sorteos de los
/// mockups son solo referencia visual). Este servicio es el punto
/// único donde se conectará el backend después
/// (docs/NOTAS_REPORTES.md).
/// </summary>
public class EstadoCuentaService
{
    /// <summary>
    /// Construye el Estado de Cuenta con valores en cero y sin sorteos.
    /// </summary>
    public Task<EstadoCuenta> ObtenerAsync()
    {
        var estado = new EstadoCuenta
        {
            FechaEmision = DateTime.Today,
            Vendedor = string.Empty,
            Plaza = string.Empty,
            FondoDeAhorro = 0m,
            Fideicomiso = 0m,
            Pagares = 0m,
            BolsaElectronica = 0m,
            GarantiaTotal = 0m,
            CapacidadDeCredito = 0m,
        };
        return Task.FromResult(estado);
    }
}
