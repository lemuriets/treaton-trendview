using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4B1, "Минутная вентиляция")]
public class IdMMvCivl : BasePackageParsed
{
    public const int Id = 0x4B1;

    public IdMMvCivl(CanPackage package, string name) : base(package, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }
        var span = Data.Span;

        var mvTotal = BitUtil.ToU16(span[0], span[1]);
        var mvMachine = BitUtil.ToU16(span[2], span[3]);
        
        var numericData = new NumericDataItem[]
        {
            new("mvTotal", mvTotal),
            new("mvMachine", mvMachine)
        };

        return new PackageData(numericData, []);
    }
}