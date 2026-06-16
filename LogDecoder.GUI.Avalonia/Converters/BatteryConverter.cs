using System.Globalization;
using Avalonia.Data.Converters;
using LogDecoder.GUI.Avalonia;

namespace LogDecoder.GUI.Avalonia.Converters;

public class BatteryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d && !double.IsNaN(d))
        {
            return d > 100.0 ? Localizer.L("BatteryCharging") : d.ToString("F2", culture);
        }

        return "---";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
