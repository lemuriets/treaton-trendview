using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4C8, "Графическая информация (VCO2)")]
public class IdWaveVco2Capno2 : BasePackageParsed
{
    public const int Id = 0x4C8;

    public IdWaveVco2Capno2(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }

        var span = Data.Span;

        var vco2Wave = BitUtil.ToU16(span[0], span[1]) * 0.01;  // 0.01 мл
        var vco2Integral = BitUtil.ToU16(span[2], span[3]);  // %

        var numericData = new NumericDataItem[]
        {
            new("VCO2Wave_ml_x100", vco2Wave),
            new("VCO2Integral_percent", vco2Integral)
        };
        var messages = Array.Empty<string>();

        return new PackageData(numericData, messages);
    }
}
