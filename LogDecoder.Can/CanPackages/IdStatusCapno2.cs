using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4C3, "Состояние ПП")]
public class IdStatusCapno2 : BasePackageParsed
{
    public const int Id = 0x4C3;

    // TODO: добавить кодов и доработать парсинг
    private static readonly Dictionary<int, (string, PackageTechStatus)> BitsDefinitions = new()
    {
        { 0,  ("Отрицательное значение CO2 (ошибка нуля/адаптера)", PackageTechStatus.Warning) },
        { 1,  ("Ошибка адаптера (загрязнён/неправильный тип)", PackageTechStatus.Warning) },
        { 2,  ("Обнаружено дыхание", PackageTechStatus.Ok) },
        { 3,  ("Высокое CO2 (предел измерения) — игнорировать", PackageTechStatus.Warning) },
        { 4,  ("Капнограф не может начать калибровку (см. биты 10,11 и др.)", PackageTechStatus.Warning) },
        { 5,  ("Активен режим сна", PackageTechStatus.Ok) },
        { 8,  ("Нестабильная/неустойчивая температура", PackageTechStatus.Warning) },
        { 9,  ("Нестабильная/неустойчивая температура (старший)", PackageTechStatus.Warning) },
        { 10, ("Ошибка/событие, связанное с калибровкой", PackageTechStatus.Warning) },
        { 11, ("Ошибка/событие, связанное с калибровкой (старший)", PackageTechStatus.Warning) },

        { 13, ("Проблемы со стабилизацией питания / нестабильное питание", PackageTechStatus.Error) },
        { 14, ("Проблемы со стабилизацией питания / ограничение тока", PackageTechStatus.Error) },

        { 16, ("Неиспользуемый бит (зарезервирован)", PackageTechStatus.Ok) },
        { 21, ("Аппаратная ошибка", PackageTechStatus.Error) },
        { 22, ("EEPROM неисправна", PackageTechStatus.Error) },
        { 24, ("Отсутствует адаптер бокового потока", PackageTechStatus.Error) },
        { 25, ("Насос выработал ресурс", PackageTechStatus.Error) },
        { 26, ("Ошибка пневматики", PackageTechStatus.Error) },
        { 27, ("Аппаратная ошибка (общее)", PackageTechStatus.Error) },
    };

    public IdStatusCapno2(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }
        var statusBits = BitUtil.ToU32(Data[0], Data[1], Data[2], Data[3]);

        var priority = Data[4];
        var presence = Data[5];
        var atm = BitUtil.ToU16(Data[6], Data[7]);

        var messages = Array.Empty<string>();
        if (presence == 0x00)
        {
            messages = ParseBits(statusBits, BitsDefinitions);
        }
        var numericData = new NumericDataItem[]
        {
            new("status_bits", statusBits),
            new("priority", priority),
            new("presence", presence),
            new("atmospheric_pressure", atm)
        };

        return new PackageData(numericData, messages);
    }
}