using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LogDecoder.GUI.Avalonia.Converters;

/// <summary>true =&gt; underline decoration (clickable link), otherwise none.</summary>
public sealed class BoolToUnderlineConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? TextDecorations.Underline : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
