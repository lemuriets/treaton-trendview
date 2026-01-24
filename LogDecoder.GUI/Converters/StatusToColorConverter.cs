using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LogDecoder.CAN.Packages;

namespace LogDecoder.GUI.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PackageTechStatus status)
        {
            return status switch
            {
                PackageTechStatus.Ok       => Brushes.LightGray,
                PackageTechStatus.Info     => Brushes.DeepSkyBlue,
                PackageTechStatus.Warning  => Brushes.Gold,
                PackageTechStatus.Error    => Brushes.OrangeRed,
                PackageTechStatus.Critical => Brushes.Red,
                _ => Brushes.White
            };
        }
        return Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}