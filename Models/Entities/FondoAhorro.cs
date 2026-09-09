using System.Collections.ObjectModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Reporte de Fondo de Ahorro (pestaña 2 de Reportes, mockups 9.2).
/// Fase SOLO INTERFAZ: saldos en cero y movimientos SIN registros —
/// la estructura queda lista para recibir los datos reales del
/// backend (docs/NOTAS_FONDO_AHORRO.md).
/// </summary>
public class FondoAhorro
{
    /// <summary>Inicio del periodo seleccionado por el usuario.</summary>
    public DateTime FechaInicio { get; set; }

    /// <summary>Fin del periodo seleccionado por el usuario.</summary>
    public DateTime FechaFin { get; set; }

    /// <summary>Nombre del titular/cliente (vendrá de la sesión real).</summary>
    public string Titular { get; set; } = string.Empty;

    /// <summary>Saldo inicial del periodo.</summary>
    public decimal SaldoInicial { get; set; }

    /// <summary>Suma de depósitos del periodo.</summary>
    public decimal Depositos { get; set; }

    /// <summary>Suma de retiros del periodo.</summary>
    public decimal Retiros { get; set; }

    /// <summary>Saldo final del periodo.</summary>
    public decimal SaldoFinal { get; set; }

    /// <summary>
    /// Movimientos del periodo (tabla del PDF: Fecha, Folio, Origen,
    /// Monto). Fase solo-interfaz: VACÍA por directriz del usuario
    /// (sin mocks ni seeds); se llenará desde el backend.
    /// </summary>
    public ObservableCollection<MovimientoFondoAhorro> Movimientos { get; } = new();

    /// <summary>Total acumulado del periodo (para la barra roja del PDF).</summary>
    public decimal Total => Movimientos.Sum(m => m.Monto);
}
