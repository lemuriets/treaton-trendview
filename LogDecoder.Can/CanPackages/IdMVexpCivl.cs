using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4AD, "Измеренный объём выдоха")]
public class IdMVexpCivl : BasePackageParsed
{
    public const int Id = 0x4AD;

    public IdMVexpCivl(CanPackage package, string name) : base(package, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
            return null;
        var vexpAvg = BitUtil.ToU16(Data[0], Data[1]);
        var vexp = BitUtil.ToU16(Data[2], Data[3]);
        
        var numericData = new NumericDataItem[]
        {
            new("vexpAvg", vexpAvg),
            new("vexp", vexp)
        };
        return new PackageData(numericData, []);
    }
}