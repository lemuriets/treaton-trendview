using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x401, "Состояние источника питания")]
public class IdStatusPwr : BasePackageParsed
{
    public const int Id = 0x401;

    private static readonly Dictionary<int, BitMessage> BitsDefinitions = new()
    {
        { 0, BitMessage.One("Отсутствует напряжение в сети 220V", PackageTechStatus.Error) },
        { 1, BitMessage.One("Неисправен вентилятор", PackageTechStatus.Error) },
        { 2, BitMessage.One("Осталось менее 10 минут работы от аккумулятора", PackageTechStatus.Warning) },
        { 3, BitMessage.One("Авария АКБ: АКБ отсутствует", PackageTechStatus.Warning) },
        { 4, BitMessage.One("Авария АКБ: превышение тока зарядки/КЗ", PackageTechStatus.Warning) },
        { 5, BitMessage.One("Превышение напряжения зарядки аккумулятора", PackageTechStatus.Warning) },
        { 6, BitMessage.One("Неисправность ключа зарядного устройства", PackageTechStatus.Warning) },
        { 7, BitMessage.One("Неисправен динамик (обрыв)", PackageTechStatus.Warning) },
        { 8, BitMessage.One("подключено внешнее питание") },
        { 9, BitMessage.One("\"плохой\" внешний источник питания", PackageTechStatus.Warning) },
        { 15, BitMessage.One("Рестарт") },
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

