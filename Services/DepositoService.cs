using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Estado en memoria del flujo Depósitos (mockups 7.x de Gestión). Fase
/// solo-interfaz: nada se persiste; la lista arranca vacía y solo crece
/// al guardar depósitos reales capturados por el usuario. Los catálogos
/// (bancos) son datos del mockup, NO seeds de registros.
/// </summary>
public class DepositoService
{
    /// <summary>
    /// Instituciones del selector (mockup 7.2 "Seleccionar Banco"; fila 3
    /// ilegible, definida como BBVA por el usuario). Fuente real: backend,
    /// catálogo de cuentas bancarias (ver docs/NOTAS_DEPOSITOS.md).
    /// </summary>
    public static readonly string[] Bancos =
    {
        "BANAMEX",
        "BANCO AZTECA",
        "BBVA",
        "INBURSA",
        "SANTANDER",
    };

    private readonly List<Deposito> _depositos = new();

    /// <summary>Depósitos guardados (más reciente primero).</summary>
    public IReadOnlyList<Deposito> Depositos => _depositos;

    /// <summary>Folio consecutivo del siguiente depósito.</summary>
    public int SiguienteFolio => _depositos.Count + 1;

    /// <summary>Registra el depósito y devuelve el folio asignado.</summary>
    public int Guardar(Deposito deposito)
    {
        deposito.Folio = SiguienteFolio;
        deposito.RegistradoEnUtc = DateTime.UtcNow;
        _depositos.Insert(0, deposito);
        return deposito.Folio;
    }
}
