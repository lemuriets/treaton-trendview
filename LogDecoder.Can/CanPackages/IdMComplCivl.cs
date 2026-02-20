using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4B4, "Комплайнс и резистанс")]
public class IdMComplCivl : BasePackageParsed
{
    public const int Id = 0x4B4;

    public IdMComplCivl(CanPackage package, string name) : base(package, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }
        var cst = BitUtil.ToU16(Data[0], Data[1]) * 0.1;
        var rst = BitUtil.ToU16(Data[2], Data[3]);
        
        var numericData = new NumericDataItem[]
        {
            new("cst", cst),
            new("rst", rst)
        };

        return new PackageData(numericData, []);
    }
}