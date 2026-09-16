using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Models.Responses;

/// <summary>
/// Resultado de GET api/mobile/expendios (campo "data"): conjunto de
/// expendios (credenciales) del billetero autenticado + estado COMPARTIDO
/// de Permisos de Venta (viven en `billeteros`, no por expendio).
/// </summary>
public class MobileExpendiosResponse
{
    /// <summary>Expendios (credenciales) del mismo billetero, orden id ascendente.</summary>
    public List<ExpendioItem>? Expendios { get; set; }

    /// <summary>Estado compartido de permisos del billetero (para refrescar la sesión).</summary>
    public BilleteroMobile? Billetero { get; set; }
}

/// <summary>
/// Resultado de PUT api/mobile/expendios/{id}/permisos: el expendio usado
/// como puerta + el billetero con los permisos YA actualizados.
/// </summary>
public class MobilePermisosResponse
{
    public int IdExpendio { get; set; }

    public BilleteroMobile? Billetero { get; set; }
}
