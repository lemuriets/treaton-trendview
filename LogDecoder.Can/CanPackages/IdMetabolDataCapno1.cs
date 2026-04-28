using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x490, "Измеренные параметры метаболизма")]
public class IdMetabolDataCapno1 : BasePackageParsed
{
    public const int Id = 0x490;

    public IdMetabolDataCapno1(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }

        var span = Data.Span;

        var vco2 = BitUtil.ToU16(span[0], span[1]);
        var vo2 = BitUtil.ToU16(span[2], span[3]);
        var rq = BitUtil.ToU16(span[4], span[5]) * 0.01;
        var ree = BitUtil.ToU16(span[6], span[7]);


        var numericData = new NumericDataItem[]
        {
            new("VCO2_ml_min", vco2),
            new("VO2_ml_min", vo2),
            new("RQ_x100", rq),
            new("REE_kcal_day", ree),
        };
        var messages = new List<string>();

        return new PackageData(numericData, messages);
    }
}
