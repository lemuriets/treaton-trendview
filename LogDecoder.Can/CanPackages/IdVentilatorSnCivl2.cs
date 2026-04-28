using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x504, "Информация о серийном номере аппарата")]
public class IdVentilatorSnCivl2 : BasePackageParsed
{
    public const int Id = 0x504;

    public IdVentilatorSnCivl2(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }

        var span = Data.Span;

        var serial = BitUtil.ToU32(span[0], span[1], span[2], span[3]);

        var numericData = new NumericDataItem[]
        {
            new("SerialNumber", serial)
        };
        var messages = new List<string>();

        return new PackageData(numericData, messages);
    }
}
