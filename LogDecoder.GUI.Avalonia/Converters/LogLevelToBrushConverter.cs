using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Serilog.Events;

namespace LogDecoder.GUI.Avalonia.Converters;

public class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is LogEventLevel level
            ? level switch
            {
                LogEventLevel.Verbose => Brushes.Gray,
                LogEventLevel.Debug => Brushes.DimGray,
                LogEventLevel.Information => Brushes.White,
                LogEventLevel.Warning => Brushes.Gold,
                LogEventLevel.Error => Brushes.OrangeRed,
                LogEventLevel.Fatal => Brushes.Red,
                _ => Brushes.White
            }
            : Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
