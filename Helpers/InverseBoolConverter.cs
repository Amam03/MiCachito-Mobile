using System.Globalization;

namespace MiCachito.Mobile.Helpers;

/// <summary>
/// Invierte un bool para IsVisible (patrón StringNotEmptyConverter: el
/// proyecto casi no usa converters; aquí evita una propiedad derivada
/// por fila de sesión en el DataTemplate).
///
/// Uso (M-06 Sesiones Activas): el botón Revocar de cada fila es visible
/// SOLO en sesiones DISTINTAS a la actual — la sesión con la que se
/// consulta se cierra desde Cerrar Sesión, nunca desde aquí.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return true;
        }

        return value is bool b && !b;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("InverseBoolConverter es solo OneWay");
}
