using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x401, "Состояние источника питания")]
public class IdStatusPwr : BasePackageParsed
{
    public const int Id = 0x401;

    private static readonly Dictionary<int, (string msg, PackageTechStatus status)> BitsDefinitions = new()
    {
        { 0, ("Отсутствует напряжение в сети 220V", PackageTechStatus.Error) },
        { 1, ("Неисправен вентилятор", PackageTechStatus.Error) },
        { 2, ("Осталось менее 10 минут работы от аккумулятора", PackageTechStatus.Warning) },
        { 3, ("Авария АКБ: АКБ отсутствует", PackageTechStatus.Warning) },
        { 4, ("Авария АКБ: превышение тока зарядки/КЗ", PackageTechStatus.Warning) },
        { 5, ("Превышение напряжения зарядки аккумулятора", PackageTechStatus.Warning) },
        { 6, ("Неисправность ключа зарядного устройства", PackageTechStatus.Warning) },
        { 7, ("Неисправен динамик (обрыв)", PackageTechStatus.Warning) },
        { 8, ("подключено внешнее питание", PackageTechStatus.Info) },
        { 9, ("\"плохой\" внешний источник питания", PackageTechStatus.Warning) },
        { 15, ("Рестарт", PackageTechStatus.Info) },
    };
    
    public IdStatusPwr(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 5)
        {
            return null;
        }
        var span = Data.Span;

        var battery = span[1]; 
        var statusBits = BitUtil.ToU16(span[2], span[3]);
        var alertStatus = span[4];
        
        var numericData = new NumericDataItem[]
        {
            new("battery", battery),
            new("byte4", alertStatus),
        };
        var messages = ParseBitsAndUpdateStatus(statusBits, BitsDefinitions);

        return new PackageData(numericData, messages);
    }
}

