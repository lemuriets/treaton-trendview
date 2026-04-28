using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x514, "Информация о давлении Paux (Data2)")]
public class IdPauxData2Civl2 : BasePackageParsed
{
    public const int Id = 0x514;

    public IdPauxData2Civl2(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }

        var span = Data.Span;

        var ptpi = BitUtil.ToS16(span[0], span[1]);
        var ptpe = BitUtil.ToS16(span[2], span[3]);
        var ptpm = BitUtil.ToS16(span[4], span[5]);
        var ccw = BitUtil.ToS16(span[6], span[7]);

        var numericData = new NumericDataItem[]
        {
            new("Ptpi_mmH2O", ptpi),
            new("Ptpe_mmH2O", ptpe),
            new("Ptpm_mmH2O", ptpm),
            new("Ccw_ml_cmH2O", ccw)
        };
        var messages = new List<string>();

        return new PackageData(numericData, messages);
    }
}
