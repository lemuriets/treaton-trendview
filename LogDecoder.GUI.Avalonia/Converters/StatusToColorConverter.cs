using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.GUI.Avalonia.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is PackageTechStatus status
            ? status switch
            {
                PackageTechStatus.Ok => Brushes.LightGray,
                PackageTechStatus.Info => Brushes.DeepSkyBlue,
                PackageTechStatus.Warning => Brushes.Gold,
                PackageTechStatus.Error => Brushes.OrangeRed,
                PackageTechStatus.Critical => Brushes.Red,
                _ => Brushes.White
            }
            : Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
