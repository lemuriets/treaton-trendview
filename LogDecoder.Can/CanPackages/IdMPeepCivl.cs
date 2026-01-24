using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4AE, "PEEP, AutoPEEP и поток на вдохе")]
public class IdMPeepCivl : BasePackageParsed
{
    public const int Id = 0x4AE;

    public IdMPeepCivl(CanPackage package, string name) : base(package, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 6)
        {
            return null;
        }
        var peep = BitUtil.ToU16(Data[0], Data[1]);
        var autoPeep = BitUtil.ToU16(Data[2], Data[3]);
        var flow = (short)BitUtil.ToU16(Data[4], Data[5]) * 0.1;
        
        var numericData = new NumericDataItem[]
        {
            new("peep", peep),
            new("autoPeep", autoPeep),
            new("peepFlow", flow)
        };

        return new PackageData(numericData, []);
    }
}