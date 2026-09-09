using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Fuente del reporte de Fondo de Ahorro (pestaña 2 de Reportes).
/// Fase SOLO INTERFAZ: devuelve la estructura en cero y SIN
/// movimientos (directriz del usuario: ni mocks ni seeds; los
/// movimientos de los mockups son solo referencia visual). Este
/// servicio es el punto único donde se conectará el backend
/// después (docs/NOTAS_FONDO_AHORRO.md).
/// </summary>
public class FondoAhorroService
{
    /// <summary>
    /// Construye el reporte del periodo indicado con valores en cero
    /// y sin movimientos.
    /// </summary>
    public Task<FondoAhorro> ObtenerAsync(DateTime inicio, DateTime fin)
    {
        var fondo = new FondoAhorro
        {
            FechaInicio = inicio,
            FechaFin = fin,
            Titular = string.Empty,
            SaldoInicial = 0m,
            Depositos = 0m,
            Retiros = 0m,
            SaldoFinal = 0m,
        };
        return Task.FromResult(fondo);
    }
}
