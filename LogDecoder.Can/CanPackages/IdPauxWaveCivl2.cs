using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x512, "Информация о давлении Paux (Wave)")]
public class IdPauxWaveCivl2 : BasePackageParsed
{
    public const int Id = 0x512;

    public IdPauxWaveCivl2(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }

        var span = Data.Span;

        var paux1 = BitUtil.ToS16(span[0], span[1]);
        var paux2 = BitUtil.ToS16(span[2], span[3]);

        var numericData = new NumericDataItem[]
        {
            new("paux1_mmH2O", paux1),
            new("paux2_mmH2O", paux2)
        };
        var messages = Array.Empty<string>();

        return new PackageData(numericData, messages);
    }
}
