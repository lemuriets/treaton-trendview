using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x422, "Состояние платы CAN-Ethernet")]
public class IdStatusScm : BasePackageParsed
{
    public const int Id = 0x422;

    private static readonly Dictionary<int,(string,PackageTechStatus)> BitsDefinitions = new()
    {
        { 0, ("Ошибка PHY Ethernet", PackageTechStatus.Error) },
        { 1, ("Ethernet Link UP", PackageTechStatus.Info) },
        { 2, ("Full duplex", PackageTechStatus.Info) },
        { 3, ("100 Мбит/с", PackageTechStatus.Info) },
        { 4, ("Есть связь по управляющему порту", PackageTechStatus.Info) }
    };

    public IdStatusScm(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }
        var numericData = Array.Empty<NumericDataItem>();
        var messages = ParseBits(Data[0], BitsDefinitions);

        return new PackageData(numericData, messages);
    }
}

