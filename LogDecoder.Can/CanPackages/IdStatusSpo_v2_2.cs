using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x582, "Состояние модуля пульсометрии Masimo 2")]
public class IdStatusSpo_v2_2 : BasePackageParsed
{
    public IdStatusSpo_v2_2(CanPackage p, string name) : base(p, name) { }
    
    public const int Id = 0x582;
    
    // TODO: добавить кодов и доработать парсинг
    private static readonly Dictionary<int, (string, PackageTechStatus)> BitsDefinitions6 = new()
    {
        { 0, ("Флаг PVI exception установлен", PackageTechStatus.Warning) }
    };

    private static readonly Dictionary<int, (string, PackageTechStatus)> BitsDefinitions7 = new()
    {
        { 0, ("PI (filling) достоверно", PackageTechStatus.Ok) },
        { 1, ("PR (pulse) достоверно", PackageTechStatus.Ok) },
        { 2, ("SpO2 достоверно", PackageTechStatus.Ok) },
        { 3, ("PVI достоверно", PackageTechStatus.Ok) },
        { 4, ("SType достоверно", PackageTechStatus.Ok) }
    };

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }

        var exc2 = BitUtil.ToU16(Data[0], Data[1]);
        var dfc  = BitUtil.ToU16(Data[2], Data[3]);
        var bfc  = BitUtil.ToU16(Data[4], Data[5]);
        var b6   = Data[6];
        var b7   = Data[7];
        var mode = (b7 >> 6) & 0x03;

        var numericData = new NumericDataItem[]
        {
            new("exc2", exc2),
            new("dfc", dfc),
            new("bfc", bfc),
            new("byte6", b6),
            new("byte7", b7),
            new("mode", mode)
        };
        var messages = new List<string>();
        messages.AddRange(ParseBits(b6, BitsDefinitions6));
        messages.AddRange(ParseBits(b7, BitsDefinitions7));

        return new PackageData(numericData, messages.ToArray());
    }
}