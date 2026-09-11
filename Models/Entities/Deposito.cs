namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Registro de depósito bancario (mockups 7.x de Gestión). Fase
/// solo-interfaz: el registro se guarda en memoria y el folio es un
/// consecutivo local (fuente real: backend, ver docs/NOTAS_DEPOSITOS.md).
/// </summary>
public class Deposito
{
    /// <summary>Folio consecutivo local.</summary>
    public int Folio { get; set; }

    /// <summary>Banco seleccionado del catálogo.</summary>
    public string Banco { get; set; } = string.Empty;

    /// <summary>Fecha de depósito elegida (sin hora).</summary>
    public DateTime FechaDeposito { get; set; }

    /// <summary>Hora de depósito elegida.</summary>
    public TimeSpan HoraDeposito { get; set; }

    /// <summary>Folio/Movimiento/Autorización capturado (máx 20).</summary>
    public string FolioMovimiento { get; set; } = string.Empty;

    /// <summary>Monto capturado en el dial-pad (con centavos).</summary>
    public decimal Monto { get; set; }

    /// <summary>Observaciones (nombre del sorteo asociado, máx 50).</summary>
    public string Observaciones { get; set; } = string.Empty;

    /// <summary>Nombre del archivo adjunto del comprobante (opcional).</summary>
    public string? NombreComprobante { get; set; }

    /// <summary>Fecha/hora en que se guardó el registro.</summary>
    public DateTime RegistradoEnUtc { get; set; }

    /// <summary>Fecha y hora del depósito: "dd-MMMM-yyyy HH:mm" (es-MX fijo).</summary>
    public string FechaHoraTexto =>
        Helpers.FormatosFecha.FechaHoraLarga(FechaDeposito, HoraDeposito);

    /// <summary>Importe formateado con 2 decimales: "$1.00".</summary>
    public string MontoTexto => $"${Monto:0.00}";

    /// <summary>Línea del card: "BANCO AZTECA - $1.00" (mockup 7.3).</summary>
    public string BancoMontoTexto => $"{Banco} - {MontoTexto}";

    /// <summary>Fecha de captura: "Captura: dd-MMMM-yyyy" (es-MX fijo).</summary>
    public string CapturaTexto => $"Captura: {Helpers.FormatosFecha.FechaLarga(RegistradoEnUtc)}";
}
