using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4C4, "Графическая информация (CO2)")]
public class IdWaveCapno2 : BasePackageParsed
{
    public const int Id = 0x4C4;

    public IdWaveCapno2(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }

        var span = Data.Span;

        var co2PressureRaw = unchecked((short)BitUtil.ToU16(span[0], span[1])) * 0.01; // 0.01 мм рт. ст.
        var co2PercentRaw = unchecked((short)BitUtil.ToU16(span[2], span[3])) * 0.01;  // 0.01 %

        var numericData = new NumericDataItem[]
        {
            new("CO2_mmHg_x100", co2PressureRaw),
            new("CO2_percent_x100", co2PercentRaw)
        };

        var messages = Array.Empty<string>();

        // if (co2PressureRaw < 0)
        // {
        //     messages.Add("Отрицательное значение парциального давления CO2");
        // }
        //
        // if (co2PercentRaw < 0)
        // {
        //     messages.Add("Отрицательное значение процентного содержания CO2");
        // }

        return new PackageData(numericData, messages);
    }
}
