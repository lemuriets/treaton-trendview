using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4B2, "Время выдоха")]
public class IdMTexpCivl : BasePackageParsed
{
    public const int Id = 0x4B2;

    public IdMTexpCivl(CanPackage package, string name) : base(package, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 2)
        {
            return null;
        }
        var texp = BitUtil.ToU16(Data[0], Data[1]);
        
        var numericData = new NumericDataItem[]
        {
            new("texp", texp)
        };

        return new PackageData(numericData, []);
    }
}