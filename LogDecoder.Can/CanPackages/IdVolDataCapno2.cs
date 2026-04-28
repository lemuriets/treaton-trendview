using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4C7, "Информация о параметрах объемной капнографии")]
public class IdVolDataCapno2 : BasePackageParsed
{
    public const int Id = 0x4C7;

    public IdVolDataCapno2(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }

        var span = Data.Span;

        var vco2              = BitUtil.ToU16(span[0], span[1]); // мл/мин
        var alveolarVentMin   = BitUtil.ToU16(span[2], span[3]) * 0.1; // 0.1 л/мин
        var deadSpace         = BitUtil.ToU16(span[4], span[5]); // мл
        var alveolarVentCycle = BitUtil.ToU16(span[6], span[7]); // мл

        var numericData = new NumericDataItem[]
        {
            new("VCO2_ml_min", vco2),
            new("AlveolarVent_L_min_x10", alveolarVentMin),
            new("DeadSpace_ml", deadSpace),
            new("AlveolarVent_ml_cycle", alveolarVentCycle),
        };
        var messages = new List<string>();
        
        return new PackageData(numericData, messages);
    }
}
