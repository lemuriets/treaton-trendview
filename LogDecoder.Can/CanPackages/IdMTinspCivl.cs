using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4B3, "Время вдоха и RCinsp")]
public class IdMTinspCivl : BasePackageParsed
{
    public const int Id = 0x4B3;

    public IdMTinspCivl(CanPackage package, string name) : base(package, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }
        var tinsp = BitUtil.ToU16(Data[0], Data[1]);
        var rcInsp = BitUtil.ToU16(Data[2], Data[3]);
        
        var numericData = new NumericDataItem[]
        {
            new("tinsp", tinsp),
            new("rcInsp", rcInsp)
        };
        return new PackageData(numericData, []);
    }
}