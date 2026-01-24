using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4AF, "Частота дыхания")]
public class IdMRbCivl : BasePackageParsed
{
    public const int Id = 0x4AF;

    public IdMRbCivl(CanPackage package, string name) : base(package, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 2)
        {
            return null;
        }
        var rrTotal = Data[0];
        var rrIndependent = Data[1];
        
        var numericData = new NumericDataItem[]
        {
            new("RB1", rrTotal),
            new("RB2", rrIndependent),
        };
        return new PackageData(numericData, []);
    }
}