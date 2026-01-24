using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4B5, "Информация об утечке")]
public class IdMLeakCivl : BasePackageParsed
{
    public const int Id = 0x4B5;

    public IdMLeakCivl(CanPackage package, string name) : base(package, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }
        var pavg = BitUtil.ToS16(Data[0], Data[1]);
        var dV = BitUtil.ToS16(Data[2], Data[3]);
        var leak = BitUtil.ToS16(Data[4], Data[5]);
        var leakPct = BitUtil.ToS16(Data[6], Data[7]);

        var numericData = new NumericDataItem[]
        {
            new("pavg", pavg),
            new("dv", dV),
            new("leak", leak),
            new("leakPct", leakPct)
        };

        return new PackageData(numericData, []);
    }
}
