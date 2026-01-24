using System.Globalization;
using System.Windows.Data;

namespace LogDecoder.GUI.Converters;

public class NaNToPlaceholderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d && !double.IsNaN(d) && d != 0)
        {
            return d.ToString("F2");
        }
        return "---";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


