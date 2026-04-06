using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4B7, "Информация о времени наработки аппарата")]
public class IdWorktimeCivl : BasePackageParsed
{
    public const int Id = 0x4B7;

    public IdWorktimeCivl(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }

        var span = Data.Span;
        var worktimeMinutes = BitUtil.ToU32(span[0], span[1], span[2], span[3]); // мин;
        
        var numericData = new NumericDataItem[]
        {
            new("Worktime_min", worktimeMinutes)
        };
        var messages = Array.Empty<string>();

        return new PackageData(numericData, messages);
    }
}
