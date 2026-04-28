using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x3FF, "Информирование о выключении")]
public class IdOffPwr : BasePackageParsed
{
    public const int Id = 0x3FF;

    public IdOffPwr(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        // пакет без данных (0 байт)
        if (Data.Length != 0)
        {
            return null;
        }

        var numericData = Array.Empty<NumericDataItem>();
        var messages = new List<string>
        {
            "выключения питания"
        };

        return new PackageData(numericData, messages);
    }
}
