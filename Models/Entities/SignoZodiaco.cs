namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Opción del selector de signos zodiacales de la pantalla
/// "Agregar Boletos" para sorteos Zodiaco y Zodiaco Especial
/// (pantalla 9.2, tipos 3 y 4).
///
/// El selector desplegable muestra 13 filas: "Signo Aleatorio"
/// (equivalente a sin filtro, mismo patrón que "Cualquier ciudad"
/// de la pantalla 8) + los 12 signos.
///
/// Los nombres se muestran SIN acentos, tal como aparecen en el
/// mockup (Geminis, Cancer, Escorpion, Sagitario).
///
/// Mapeo backend (futuro): billetes_loteria.signo_nombre /
/// signo_codigo.
/// </summary>
public class SignoZodiaco
{
    /// <summary>
    /// Id del signo. 0 = "Signo Aleatorio" (sin filtro).
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Nombre tal como se muestra en el selector (ej. "Aries").
    /// </summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// True para la opción "Signo Aleatorio" (Id == 0).
    /// </summary>
    public bool EsAleatorio => Id == 0;

    /// <summary>
    /// Nombre en MAYÚSCULAS para comparar/mostrar en las filas
    /// de tiendas (ej. "ARIES").
    /// </summary>
    public string NombreCaps => Nombre.ToUpperInvariant();
}
