using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Data;

/// <summary>
/// Catálogo mock de signos zodiacales para el selector de la pantalla
/// "Agregar Boletos" en sorteos Zodiaco y Zodiaco Especial (9.2).
/// UI-only: 13 opciones transcritas verbatim del mockup
/// ("Signo Aleatorio" + 12 signos, sin acentos como en Sr. Billetero).
///
/// REGLA DE NEGOCIO: "Signo Aleatorio" (Id = 0) es SIEMPRE la primera
/// opción y equivale a sin filtro — mismo patrón que "Cualquier ciudad"
/// de la pantalla 8. Los signos van en orden zodiacal.
///
/// Futuro (integración): el signo del billete vive en
/// billetes_loteria.signo_nombre / signo_codigo.
/// </summary>
public static class SignosZodiacoData
{
    /// <summary>
    /// Opción "Signo Aleatorio" (Id = 0, sin filtro).
    /// Siempre debe ir primera en el selector.
    /// </summary>
    public static readonly SignoZodiaco SignoAleatorio = new()
    {
        Id = 0,
        Nombre = "Signo Aleatorio",
    };

    /// <summary>
    /// Los 12 signos en orden zodiacal, con los nombres del mockup
    /// (sin acentos).
    /// </summary>
    private static readonly List<SignoZodiaco> Signos = new()
    {
        new() { Id = 1,  Nombre = "Aries" },
        new() { Id = 2,  Nombre = "Tauro" },
        new() { Id = 3,  Nombre = "Geminis" },
        new() { Id = 4,  Nombre = "Cancer" },
        new() { Id = 5,  Nombre = "Leo" },
        new() { Id = 6,  Nombre = "Virgo" },
        new() { Id = 7,  Nombre = "Libra" },
        new() { Id = 8,  Nombre = "Escorpion" },
        new() { Id = 9,  Nombre = "Sagitario" },
        new() { Id = 10, Nombre = "Capricornio" },
        new() { Id = 11, Nombre = "Acuario" },
        new() { Id = 12, Nombre = "Piscis" },
    };

    /// <summary>
    /// Devuelve todas las opciones garantizando que "Signo Aleatorio"
    /// ocupe la posición 0, seguido de los 12 signos en orden zodiacal.
    /// </summary>
    public static IReadOnlyList<SignoZodiaco> ObtenerTodas()
    {
        var signos = new List<SignoZodiaco> { SignoAleatorio };
        signos.AddRange(Signos);
        return signos;
    }
}
