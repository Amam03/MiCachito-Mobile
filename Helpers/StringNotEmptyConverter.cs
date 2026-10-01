using System.Globalization;

namespace MiCachito.Mobile.Helpers;

/// <summary>
/// Indica si un string tiene contenido, para mostrar u ocultar un mensaje.
///
/// Se usa en la pantalla de Datos del Cliente (TEC): el Label de error solo
/// se dibuja cuando hay texto, en lugar de dejar un hueco con el color rojo.
///
/// Nota: el proyecto no tenia converters de UI; este es el primero y sigue
/// el patron mas simple posible (IValueConverter sin dependencias).
/// </summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(value as string);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("StringNotEmptyConverter es solo OneWay");
}