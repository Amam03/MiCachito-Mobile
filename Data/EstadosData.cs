namespace MiCachito.Mobile.Data;

/// <summary>
/// Catálogo de estados de la República Mexicana para el picker
/// "Seleccionar Estado" de la pantalla 15 (Datos del Cliente, Sorteos Tec).
///
/// Por decisión del usuario (sep-2026): las 32 entidades federativas.
/// No existe catálogo de estados en el backend ni en docs (verificado),
/// por lo que este catálogo es la fuente local de la app.
/// En producción podría venir de un endpoint de catálogos.
/// </summary>
public static class EstadosData
{
    /// <summary>Las 32 entidades federativas en orden alfabético.</summary>
    public static IReadOnlyList<string> ObtenerEstados() => _estados;

    private static readonly List<string> _estados =
    [
        "Aguascalientes",
        "Baja California",
        "Baja California Sur",
        "Campeche",
        "Chiapas",
        "Chihuahua",
        "Ciudad de México",
        "Coahuila",
        "Colima",
        "Durango",
        "Estado de México",
        "Guanajuato",
        "Guerrero",
        "Hidalgo",
        "Jalisco",
        "Michoacán",
        "Morelos",
        "Nayarit",
        "Nuevo León",
        "Oaxaca",
        "Puebla",
        "Querétaro",
        "Quintana Roo",
        "San Luis Potosí",
        "Sinaloa",
        "Sonora",
        "Tabasco",
        "Tamaulipas",
        "Tlaxcala",
        "Veracruz",
        "Yucatán",
        "Zacatecas",
    ];
}
