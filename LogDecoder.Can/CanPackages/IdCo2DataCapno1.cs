using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x485, "Измеренные параметры CO2")]
public class IdCo2DataCapno1 : BasePackageParsed
{
    public const int Id = 0x485;

    public IdCo2DataCapno1(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 5)
        {
            return null;
        }

        var span = Data.Span;

        var etCo2Percent = span[0] * 0.1; // 0.1%
        var inCo2Percent = span[1] * 0.1; // 0.1%
        var etCo2MmHg    = span[2]; // мм рт. ст.
        var inCo2MmHg    = span[3]; // мм рт. ст.
        var rr           = span[4]; // частота дыхания

        var numericData = new NumericDataItem[]
        {
            new("EtCO2_percent_x10", etCo2Percent),
            new("InspCO2_percent_x10", inCo2Percent),
            new("EtCO2_mmHg", etCo2MmHg),
            new("InspCO2_mmHg", inCo2MmHg),
            new("RespRate", rr)
        };

        var messages = new List<string>();

        // if (rr == 0)
        // {
        //     messages.Add("Капнографическое апноэ");
        // }

        return new PackageData(numericData, messages);
    }
}
