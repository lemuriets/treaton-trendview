using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x50D, "Информация о параметрах дыхания (Vexp)")]
public class IdVexpCivl2 : BasePackageParsed
{
    public const int Id = 0x50D;

    public IdVexpCivl2(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }

        var span = Data.Span;

        var vexpAvg = BitUtil.ToU32(span[0], span[1], span[2], span[3]) * 0.1; // 0.1 мл
        var vexpRaw = BitUtil.ToU32(span[4], span[5], span[6], span[7]) * 0.1; // 0.1 мл

        var numericData = new NumericDataItem[]
        {
            new("Vexp_avg_ml_x10", vexpAvg),
            new("Vexp_raw_ml_x10", vexpRaw)
        };
        var messages = new List<string>();

        return new PackageData(numericData, messages);
    }
}
