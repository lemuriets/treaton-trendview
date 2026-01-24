using System.Globalization;
using System.Windows.Data;

namespace LogDecoder.GUI.Converters;

public class BatteryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            switch (value)
            {
                case >100.0:
                    return "Заряжается";
                default:
                    return d.ToString("F2");
            }
        }
        return "---";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}