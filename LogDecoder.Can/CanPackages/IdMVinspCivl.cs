using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4AC, "Измеренный объём вдоха и удельная работа аппарата")]
public class IdMVinspCivl : BasePackageParsed
{
    public const int Id = 0x4AC;

    public IdMVinspCivl(CanPackage package, string name) : base(package, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }
        var vinsp = BitUtil.ToU16(Data[0], Data[1]);
        var work = BitUtil.ToU16(Data[2], Data[3]) * 0.01;
        
        var numericData = new NumericDataItem[]
        {
            new("vinsp", vinsp),
            new("work", work)
        };

        return new PackageData(numericData, []);
    }
}
