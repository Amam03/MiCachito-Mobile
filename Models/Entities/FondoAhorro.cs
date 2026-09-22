using System.Collections.ObjectModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Reporte de Fondo de Ahorro (pestaña 2 de Reportes, mockups 9.2).
/// Desde la Fase 2 (2026-09-22) se llena desde el backend real
/// (GET api/mobile/reportes/fondo-ahorro) vía FondoAhorroService.
/// </summary>
public class FondoAhorro
{
    /// <summary>Inicio del periodo seleccionado por el usuario.</summary>
    public DateTime FechaInicio { get; set; }

    /// <summary>Fin del periodo seleccionado por el usuario.</summary>
    public DateTime FechaFin { get; set; }

    /// <summary>Nombre del titular (billetero de la sesión, del backend).</summary>
    public string Titular { get; set; } = string.Empty;

    /// <summary>Saldo inicial del periodo (acumulado antes del rango).</summary>
    public decimal SaldoInicial { get; set; }

    /// <summary>Suma de depósitos del periodo.</summary>
    public decimal Depositos { get; set; }

    /// <summary>Suma de retiros del periodo.</summary>
    public decimal Retiros { get; set; }

    /// <summary>Saldo final del periodo (inicial + depósitos − retiros).</summary>
    public decimal SaldoFinal { get; set; }

    /// <summary>
    /// Movimientos del periodo (tabla del PDF: Fecha, Folio, Origen,
    /// Descripción, Monto). Montos CON signo (aportación +, retiro −),
    /// derivado del origen en FondoAhorroService; cronológicos.
    /// </summary>
    public ObservableCollection<MovimientoFondoAhorro> Movimientos { get; } = new();

    /// <summary>
    /// Total para la barra roja del PDF: el SALDO FINAL del periodo
    /// (mockup 9.2: Total $15,911 == Saldo final $15,911; NO la suma
    /// neta de movimientos, que ignora el saldo inicial).
    /// </summary>
    public decimal Total => SaldoFinal;
}
