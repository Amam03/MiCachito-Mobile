using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Sorteo registrado dentro de un movimiento de Premios y Reintegros
/// (mockups 3.1/3.2): tarjeta "Mayor - 3966" con flecha que expande /
/// contrae la tabla de boletos. Observable para que la flecha y la
/// tabla reaccionen en vivo (notificacion manual, sin partial OnChanged:
/// ver pitfall CS0759 del proyecto).
/// </summary>
public sealed class SorteoPremiosReintegros : INotifyPropertyChanged
{
    private bool _expandido;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string NombreSorteo { get; init; } = string.Empty;

    public ObservableCollection<BoletoPremioReintegro> Boletos { get; } = new();

    /// <summary>True = tabla de boletos desplegada (mockup 3.2 detalle).</summary>
    public bool Expandido
    {
        get => _expandido;
        set
        {
            if (_expandido == value)
            {
                return;
            }

            _expandido = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Flecha));
        }
    }

    /// <summary>Flecha del encabezado: contraido ▶ / expandido ▼.</summary>
    public string Flecha => Expandido ? "\u25BC" : "\u25B6";

    private void OnPropertyChanged([CallerMemberName] string nombre = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
    }
}
