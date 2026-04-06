using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x513, "Информация о давлении Paux (Data)")]
public class IdPauxDataCivl2 : BasePackageParsed
{
    public const int Id = 0x513;

    public IdPauxDataCivl2(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 6)
        {
            return null;
        }

        var span = Data.Span;

        var maxPressure = BitUtil.ToS16(span[0], span[1]);
        var avgPressure = BitUtil.ToS16(span[2], span[3]);
        var minPressure = BitUtil.ToS16(span[4], span[5]);

        var numericData = new NumericDataItem[]
        {
            new("PauxMax_mmH2O", maxPressure),
            new("PauxAvg_mmH2O", avgPressure),
            new("PauxMin_mmH2O", minPressure)
        };
        var messages = Array.Empty<string>();

        return new PackageData(numericData, messages);
    }
}
