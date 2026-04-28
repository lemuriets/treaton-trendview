using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x484, "Значения CO2, N2O и O2")]
public class IdWaveCapno1 : BasePackageParsed
{
    public const int Id = 0x484;

    public IdWaveCapno1(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }
        var span = Data.Span;

        var co2PressureRaw = BitUtil.ToU16(span[0], span[1]) * 0.1; // 0.1 мм рт. ст.
        var co2PercentRaw  = BitUtil.ToU16(span[2], span[3]) * 0.01; // 0.01 %
        var o2Raw          = BitUtil.ToU16(span[4], span[5]) * 0.1; // 0.1 %
        var n2oRaw         = BitUtil.ToU16(span[6], span[7]) * 0.1; // 0.1 %

        var numericData = new NumericDataItem[]
        {
            new("CO2_mmHg_x10", co2PressureRaw),
            new("CO2_percent_x100", co2PercentRaw),
            new("O2_percent_x10", o2Raw),
            new("N2O_percent_x10", n2oRaw)
        };
        var messages = new List<string>();

        return new PackageData(numericData, messages);
    }
}
