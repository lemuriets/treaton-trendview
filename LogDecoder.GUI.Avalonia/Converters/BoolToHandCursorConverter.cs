using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Input;

namespace LogDecoder.GUI.Avalonia.Converters;

/// <summary>true =&gt; hand cursor (clickable), otherwise the default cursor.</summary>
public sealed class BoolToHandCursorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
