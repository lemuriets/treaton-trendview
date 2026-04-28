using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x463, "Управление содержанием кислорода")]
public class IdOxy : BasePackageParsed
{
    public const int Id = 0x463;

    public IdOxy(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 1)
        {
            return null;
        }

        var span = Data.Span;
        var oxyPercent = span[0];

        var numericData = new NumericDataItem[]
        {
            new("FiO2_percent", oxyPercent)
        };
        var messages = new List<string>();

        return new PackageData(numericData, messages);
    }
}
