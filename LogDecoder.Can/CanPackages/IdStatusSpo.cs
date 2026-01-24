using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4E1, "Состояния модуля пульсометрии Treaton")]
public class IdStatusSpo : BasePackageParsed
{
    public const int Id = 0x4E1;

    private static readonly Dictionary<int, (string msg, PackageTechStatus level)> BitsDefinitions = new()
    {
        { 0, ("Норма (нет сообщения)", PackageTechStatus.Ok) },
        { 1, ("Модуль отключен", PackageTechStatus.Info) },
        { 2, ("Модуль неисправен", PackageTechStatus.Error) },
        { 3, ("Датчик отсоединен", PackageTechStatus.Warning) },
        { 4, ("Датчик неисправен", PackageTechStatus.Error) },
        { 5, ("Датчик сброшен", PackageTechStatus.Info)},
        { 6, ("Слабый сигнал датчика ФПГ", PackageTechStatus.Warning)},
        { 7, ("Деградация датчика", PackageTechStatus.Warning)}
    };

    public IdStatusSpo(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Length < 6)
        {
            return null;
        }
        // 0.1%
        var pleth = BitUtil.ToU16(Data[0], Data[1]);
        // уд/мин
        var pulseRate = BitUtil.ToU16(Data[2], Data[3]);
        var spO2 = Data[4];
        
        var numericData = new NumericDataItem[]
        {
            new("pleth", pleth),
            new("pulseRate", pulseRate),
            new("spO2", spO2)
        };;
        var messages = ParseBits(Data[5], BitsDefinitions);

        return new PackageData(numericData, messages);
    }
}
