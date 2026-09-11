using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Data;

/// <summary>
/// DATOS TEMPORALES DE DEMOSTRACIÓN (fase SOLO INTERFAZ, mockups Expendios 1-6).
///
/// Propósito: probar visualmente la relación usuario↔expendio y la edición de
/// Permisos de Venta (fuente de verdad de los botones de la pestaña Vender).
///
/// Reglas (spec §12-§15):
///  - UN SOLO expendio de prueba, asociado al usuario "billetero" (directriz
///    del usuario 2026-09-11): sirve para probar edición y, después, creación
///    de otro expendio. NO agregar más expendios ni usuarios de prueba.
///  - NO es seed permanente, NO modifica BD, NO son datos reales del backend.
///  - El contrato del backend aún no existe; esta clase es la ÚNICA fuente de
///    datos de la fase UI y será sustituida por el servicio real sin tocar
///    ViewModels ni Views.
///  - Titular y domicilio verbatim del mockup 4.
/// </summary>
public static class ExpendiosDemoData
{
    /// <summary>Colección en memoria compartida (las pantallas mutan las MISMAS instancias).</summary>
    private static readonly List<Expendio> _expendios =
    [
        new Expendio
        {
            Id = 1,
            Alias = "Centro",
            Nombre = "DEMETRIO HECTOR CELIS VELEZ",
            Usuario = "billetero",
            Contrasena = "demo1234",
            Domicilio = "CALLE 214 C, SAN JERONIMO",
            // Permisos iniciales: los tres productos (se editan desde el form).
            Permisos = new PermisosVenta { SorteosTec = true, TiempoAire = true, Lotenal = true },
        },
    ];

    /// <summary>Expendios del usuario actual (demo). UNO solo (billetero) para probar
    /// edición/creación; al crear desde el form se agregan en memoria.</summary>
    public static List<Expendio> ObtenerExpendios() => _expendios;

    /// <summary>Siguiente Id provisional (solo fase UI; el real lo asignará el backend).</summary>
    public static int SiguienteId() => _expendios.Count == 0
        ? 1
        : _expendios.Max(e => e.Id) + 1;

    /// <summary>Alta de un expendio creado desde el formulario (en memoria, sin BD).</summary>
    public static void Agregar(Expendio expendio) => _expendios.Add(expendio);
}
