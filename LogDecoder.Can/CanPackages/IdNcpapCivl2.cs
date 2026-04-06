using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x518, "Информация о давлении nCPAP")]
public class IdNcpapCivl2 : BasePackageParsed
{
    public const int Id = 0x518;

    public IdNcpapCivl2(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 2)
        {
            return null;
        }
        var span = Data.Span;
        
        var ncpap = BitUtil.ToU16(span[0], span[1]);

        var numericData = new NumericDataItem[]
        {
            new("nCPAP_mmH2O", ncpap)
        };
        var messages = Array.Empty<string>();

        return new PackageData(numericData, messages);
    }
}
