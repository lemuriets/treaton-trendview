using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.Exceptions;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(1120, "Синхро-пакет")]
public class IdSynchro : BasePackageParsed
{
    public IdSynchro(CanPackage package, string name): base(package, name) { }
    
    public static int Id = 1120;
    public const string Name = "Синхро-пакет";

    public override PackageData? ParseData()
    {
        if (Data.Length < 6)
        {
            return null;
        }
        try
        {
            var (year, month, day, hour, minute, second) =
                (2000 + Data[0], Data[1], Data[2], Data[3], Data[4], Data[5]);
            var datetime = new DateTime(year, month, day, hour, minute, second);

            var messages = new []{datetime.ToString()};
            var numericData = Array.Empty<NumericDataItem>();

            return new PackageData(numericData, messages);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}