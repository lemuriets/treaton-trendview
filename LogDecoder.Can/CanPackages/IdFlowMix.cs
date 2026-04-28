using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x445, "Потоки воздух/кислород")]
public class IdFlowMix : BasePackageParsed
{
    public IdFlowMix(CanPackage p, string name) : base(p, name) { }

    public const int Id = 0x445;

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }
        var airRaw = BitConverter.ToInt16(Data.Span.Slice(0, 2));
        var o2Raw  = BitConverter.ToInt16(Data.Span.Slice(2, 2));

        var airFlow = airRaw * 0.01;
        var o2Flow  = o2Raw * 0.01;

        var messages = new List<string>();
        var numericData = new[]
        {
            new NumericDataItem("AirFlow", airFlow),
            new NumericDataItem("O2Flow", o2Flow)
        };

        return new PackageData(numericData, messages);
    }
}
