using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4C5, "Измеренные параметры CO2, относящиеся к фазам ВДОХ, ВЫДОХ и частота дыхания")]
public class IdDataCapno2 : BasePackageParsed
{
    public const int Id = 0x4C5;

    public IdDataCapno2(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 5)
        {
            return null;
        }

        var span = Data.Span;

        var inspCo2Percent = span[0];
        var inspCo2MmHg = span[1];
        var etCo2Percent = span[2];
        var etCo2MmHg = span[3];
        var rr = span[4];

        var numericData = new NumericDataItem[]
        {
            new("InspCO2_percent_x10", inspCo2Percent),
            new("InspCO2_mmHg", inspCo2MmHg),
            new("EtCO2_percent_x10", etCo2Percent),
            new("EtCO2_mmHg", etCo2MmHg),
            new("RespRate_1_min", rr),
        };
        var messages = Array.Empty<string>();

        return new PackageData(numericData, messages);
    }
}
