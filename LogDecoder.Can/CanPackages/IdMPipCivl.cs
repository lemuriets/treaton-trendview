using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4B0, "Пиковое давление и стресс-индекс")]
public class IdMPipCivl : BasePackageParsed
{
    public const int Id = 0x4B0;

    public IdMPipCivl(CanPackage package, string name) : base(package, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 5)
        {
            return null;
        }
        var span = Data.Span;

        var pip = span[0];
        var cdyn = BitUtil.ToU16(span[1], span[2]) * 0.01;
        var stress = BitUtil.ToU16(span[3], span[4]) * 0.01;
        
        var numericData = new NumericDataItem[]
        {
            new("pip", pip),
            new("cdyn", cdyn),
            new("stress", stress)
        };
        return new PackageData(numericData, []);
    }
}