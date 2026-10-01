using System.Text.Json;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Models;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Persiste el carrito de venta TEC para que sobreviva a que la app se
/// cierre a mitad del flujo.
///
/// Sigue el mismo patrón que <see cref="SessionService"/>: un único blob
/// JSON en SecureStorage (clave <see cref="StorageKeys.CarritoTec"/>), con
/// una copia en memoria para no ir al disco en cada tecla.
///
/// El carrito va ACOMPAÑADO del id de billetero: si el dispositivo cambia de
/// usuario, el carrito del anterior no se muestra ni se intenta vender.
/// </summary>
public class CarritoTecService
{
    private static readonly JsonSerializerOptions StorageOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private CarritoTecState? _estado;
    private bool _cargado;

    /// <summary>
    /// Estado en memoria (null si no hay carrito o no se ha cargado).
    /// </summary>
    public IReadOnlyList<CarritoTecItem> Billetes => _estado?.Billetes ?? (IReadOnlyList<CarritoTecItem>)Array.Empty<CarritoTecItem>();

    /// <summary>True si hay boletos en el carrito.</summary>
    public bool HayBilletes => Billetes.Count > 0;

    /// <summary>
    /// Carga el carrito persistido. Si pertenece a otro billetero se
    /// descarta: un carrito de otro usuario no se debe mostrar.
    /// </summary>
    public async Task CargarAsync(int idBilletero)
    {
        if (_cargado)
        {
            // Ya se cargo en este proceso: si el billetero no coincide,
            // se limpia para no mezclar carritos de usuarios distintos.
            if (_estado is not null && _estado.IdBilletero != idBilletero)
            {
                await VaciarAsync();
            }

            return;
        }

        _cargado = true;

        string? json = await SecureStorage.Default.GetAsync(StorageKeys.CarritoTec).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            CarritoTecState? estado = JsonSerializer.Deserialize<CarritoTecState>(json, StorageOptions);
            if (estado is not null && estado.IdBilletero == idBilletero && estado.Billetes.Count > 0)
            {
                _estado = estado;
            }
        }
        catch (JsonException)
        {
            // Carrito corrupto o de una version anterior: se descarta en vez
            // de fallar al abrir la pantalla de venta.
            await VaciarAsync();
        }
    }

    /// <summary>
    /// Agrega o quita un boleto del carrito y persiste el resultado.
    /// </summary>
    public async Task AlternarAsync(int idBilletero, CarritoTecItem item)
    {
        _estado ??= new CarritoTecState { IdBilletero = idBilletero };

        CarritoTecItem? existente = _estado.Billetes.FirstOrDefault(b => b.IdBillete == item.IdBillete);
        if (existente is not null)
        {
            _estado.Billetes.Remove(existente);
        }
        else
        {
            _estado.Billetes.Add(item);
        }

        await GuardarAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Quita un boleto del carrito y persiste el cambio. Lo usa el botón
    /// Eliminar del carrito (pantalla 14).
    /// </summary>
    public async Task QuitarAsync(int idBillete)
    {
        if (_estado is null)
        {
            return;
        }

        CarritoTecItem? existente = _estado.Billetes.FirstOrDefault(b => b.IdBillete == idBillete);
        if (existente is null)
        {
            return;
        }

        _estado.Billetes.Remove(existente);
        await GuardarAsync().ConfigureAwait(false);
    }

    /// <summary>Ids de los boletos del carrito, en orden de selección.</summary>
    public IReadOnlyList<int> IdsBilletes() => _estado?.Billetes.Select(b => b.IdBillete).ToList()
        ?? (IReadOnlyList<int>)Array.Empty<int>();

    /// <summary>
    /// Vacía el carrito (tras una venta confirmada) y borra lo persistido.
    /// </summary>
    public async Task VaciarAsync()
    {
        _estado = null;
        SecureStorage.Default.Remove(StorageKeys.CarritoTec);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task GuardarAsync()
    {
        if (_estado is null || _estado.Billetes.Count == 0)
        {
            SecureStorage.Default.Remove(StorageKeys.CarritoTec);
            return;
        }

        string json = JsonSerializer.Serialize(_estado, StorageOptions);
        await SecureStorage.Default.SetAsync(StorageKeys.CarritoTec, json).ConfigureAwait(false);
    }
}