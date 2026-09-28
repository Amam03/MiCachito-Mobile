namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Modo de captura de una devolución (mockup 6.2). Cada código escaneado
/// se cuenta según el modo activo y su equivalencia en cachitos:
/// Cachito = 1, Tira = 5, Serie = 20 (ver docs/NOTAS_DEVOLUCION.md).
/// </summary>
public enum ModoCapturaDevolucion
{
    Series,
    Tiras,
    Cachitos,
}

/// <summary>
/// Fila del desglose de una devolución (mockup 6.2 desglose): un código
/// escaneado en un modo concreto. Tabla: Billete | Cantidad | Vigésimo | Serie.
/// Fase solo-interfaz: vive en memoria en DevolucionService.
/// </summary>
public sealed class FilaDesglose
{
    /// <summary>Modo en el que se capturó la fila.</summary>
    public ModoCapturaDevolucion Modo { get; init; }

    /// <summary>Código completo escaneado (identidad para dedupe).</summary>
    public string CodigoCompleto { get; init; } = string.Empty;

    /// <summary>Número de billete del código (columna Billete).</summary>
    public string Billete { get; init; } = "-";

    /// <summary>Cantidad que aporta la fila según su modo (1/5/20).</summary>
    public int Cantidad { get; init; }

    /// <summary>Vigésimo/fracción del código (columna Vigésimo).</summary>
    public string Vigesimo { get; init; } = "-";

    /// <summary>Serie o signo del código (columna Serie).</summary>
    public string Serie { get; init; } = "-";

    /// <summary>Fecha del sorteo leída del código (validación vs sorteo elegido).</summary>
    public string? FechaSorteoCodigo { get; init; }

    // ============ Campos para el POST de devolución (F4) ============

    /// <summary>
    /// Fracción/vigésimo NUMÉRICA del código (null si no trae). El backend
    /// desambigua el billete exacto por numero_billete+serie+fracción+signo
    /// +dotación.
    /// </summary>
    public int? FraccionNum { get; init; }

    /// <summary>Código de signo zodiacal del código (null si no es zodiaco).</summary>
    public string? SignoCodigo { get; init; }

    /// <summary>Serie FÍSICA del código ("01"; null en zodiaco). Ojo: la columna
    /// Serie de la tabla muestra serie o signo según el sorteo.</summary>
    public string? SerieFisica { get; init; }

    /// <summary>Dotación de la devolución en curso (billetes_loteria.numero_sorteo).</summary>
    public string? NumeroSorteo { get; init; }

    /// <summary>Id del sorteo de la devolución en curso.</summary>
    public int IdSorteo { get; init; }
}

/// <summary>
/// Devolución registrada (lista mockup 6): folio, sorteo y filas capturadas.
/// Fase solo-interfaz: vive en memoria en DevolucionService (ningún seed).
/// </summary>
public sealed class Devolucion
{
    public int Folio { get; init; }

    /// <summary>
    /// Folio REAL del movimiento devuelto por el backend (F4,
    /// "MA-20260928-0002"; vacío en registros de la fase UI-only).
    /// </summary>
    public string FolioTexto { get; init; } = string.Empty;

    /// <summary>
    /// Estatus administrativo del movimiento ("pendiente" hasta la
    /// liquidación en el flujo Desktop; vacío en registros UI-only).
    /// </summary>
    public string Estatus { get; init; } = string.Empty;

    /// <summary>Sorteo activo elegido (id + nombre corto para el header).</summary>
    public int IdSorteo { get; init; }

    public string NombreSorteo { get; init; } = string.Empty;

    /// <summary>Fecha de celebración del sorteo (texto) para el registro.</summary>
    public string FechaSorteo { get; init; } = string.Empty;

    public DateTime RegistradaEnUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Resumen corto para la fila de la lista: "C: 3 · T: 1 · S: 0".</summary>
    public string ResumenText
    {
        get
        {
            int c = Filas.Count(f => f.Modo == ModoCapturaDevolucion.Cachitos);
            int t = Filas.Count(f => f.Modo == ModoCapturaDevolucion.Tiras);
            int s = Filas.Count(f => f.Modo == ModoCapturaDevolucion.Series);
            return $"Cachitos: {c}   Tiras: {t}   Series: {s}";
        }
    }

    public List<FilaDesglose> Filas { get; } = new();
}
