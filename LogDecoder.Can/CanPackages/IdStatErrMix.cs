using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x450, "Информация о причине неисправности модуля")]
public class IdStatErrMix : BasePackageParsed
{
    public const int Id = 0x450;

    public IdStatErrMix(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }
        var span = Data.Span;

        var errorBitNumber = BitUtil.ToU16(span[0], span[1]);
        var errorVoltageMv = BitUtil.ToU16(span[2], span[3]);

        var numericData = new NumericDataItem[]
        {
            new("error_bit_number", errorBitNumber),
            new("error_voltage_mV", errorVoltageMv)
        };
        var messages = new List<string>();

        return new PackageData(numericData, messages);
    }
}