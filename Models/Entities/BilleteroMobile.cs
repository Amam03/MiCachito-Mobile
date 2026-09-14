namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Billetero del expendio autenticado. Coincide con datosBilletero() del
/// AuthController mobile (backend-integracion.md §3): perfil + Permisos de
/// Venta (tiene_tiempo_aire / tiene_prod_digitales) + reglas comerciales.
/// Los decimales llegan como string SQL ("12.50") y se conservan tal cual
/// para mostrarlos; parseo numérico solo si se necesita calcular.
/// </summary>
public class BilleteroMobile
{
    public int IdBilletero { get; set; }

    public string? ClaveBilletero { get; set; }

    public string? NombreCompleto { get; set; }

    public string? Estatus { get; set; }

    /// <summary>Permiso de venta Tiempo Aire (Permisos de Venta).</summary>
    public int TieneTiempoAire { get; set; }

    /// <summary>Permiso de venta Sorteos Tec (Permisos de Venta).</summary>
    public int TieneProdDigitales { get; set; }

    public string? ComisionTiempoAire { get; set; }

    public string? LimiteVentaDiario { get; set; }

    public string? ComisionPorcentaje { get; set; }

    public string? RetencionIsr { get; set; }

    public string? FondoAhorro { get; set; }

    public string? LimiteCredito { get; set; }
}
