using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.Exceptions;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(1120, "Синхро-пакет")]
public class IdSynchro : BasePackageParsed
{
    public IdSynchro(CanPackage package, string name): base(package, name) { }
    
    public static int Id = 1120;

    public override PackageData? ParseData()
    {
        if (Data.Length < 6)
        {
            return null;
        }
        try
        {
            var span = Data.Span;

            var (year, month, day, hour, minute, second) =
                (2000 + span[0], span[1], span[2], span[3], span[4], span[5]);
            var datetime = new DateTime(year, month, day, hour, minute, second);

            var messages = new []{datetime.ToString(CanConfig.TimeFormat)};
            var numericData = Array.Empty<NumericDataItem>();

            return new PackageData(numericData, messages);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}