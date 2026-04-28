using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x50E, "Параметры вентиляции 5")]
public class IdPar5Civl2 : BasePackageParsed
{
    public IdPar5Civl2(CanPackage p, string name) : base(p, name) { }

    public const int Id = 0x50E;

    public override PackageData? ParseData()
    {
        if (Data.Length < 7)
        {
            return null;
        }
        var span = Data.Span;

        var volume = BitUtil.ToU16(span[0], span[1]) * 0.1;
        var flow = BitUtil.ToU16(span[2], span[3]) * 0.1;
        var rb = span[4];
        var vsupp = BitUtil.ToU16(span[5], span[6]) * 0.1;

        var messages = new List<string>();
        var numericData = new NumericDataItem[]
        {
            new("Объём вдоха [0.1 мл]", volume),
            new("Заданный поток [0.1 л/мин]", flow),
            new("Величина RB [1/мин]", rb),
            new("Величина объёма поддержки Vsupp [0.1 мл]", vsupp),
        };

        return new PackageData(numericData, messages);
    }
}