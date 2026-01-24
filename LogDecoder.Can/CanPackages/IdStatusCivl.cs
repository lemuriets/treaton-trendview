using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4A8, "Состояние КИВЛ")]
public class IdStatusCivl : BasePackageParsed
{
    public IdStatusCivl(CanPackage p, string name) : base(p, name) { }
    
    public const int Id = 0x4A8;

    private static readonly Dictionary<int,(string,PackageTechStatus)> BitsDefinitions = new()
    {
        { 41, ("Подключён внешний модуль потока", PackageTechStatus.Info) },
        { 40, ("Рестарт КИВЛ", PackageTechStatus.Info) },
        { 39, ("Ошибка автокалибровки датчика кислорода", PackageTechStatus.Warning) },
        { 38, ("Идёт автокалибровка датчика кислорода", PackageTechStatus.Info) },
        { 37, ("Манёвр раскрытия альвеол", PackageTechStatus.Info) },
        { 36, ("Сбой в памяти U0 датчика кислорода", PackageTechStatus.Error) },
        { 35, ("Ошибка калибр. коэффициента датчика кислорода", PackageTechStatus.Error) },
        { 34, ("Неисправность генератора потока (высокая мощность)", PackageTechStatus.Error) },
        { 33, ("Неисправность ИОН AD7738", PackageTechStatus.Error) },
        { 32, ("Ошибка передачи по CAN", PackageTechStatus.Error) },
        { 31, ("Работает небулайзер", PackageTechStatus.Info) },
        { 30, ("Нет связи с СГ", PackageTechStatus.Error) },
        { 29, ("Неисправность ΔP-датчика (клапан выдоха)", PackageTechStatus.Error) },
        { 28, ("Неисправность датчика давления (магистраль выдоха)", PackageTechStatus.Error) },
        { 27, ("Неисправность датчика давления (магистраль вдоха)", PackageTechStatus.Error) },
        { 26, ("Неисправность АЦП AD7738", PackageTechStatus.Error) },
        { 25, ("Неисправность датчика кислорода", PackageTechStatus.Error) },
        { 24, ("Неисправность клапана выдоха", PackageTechStatus.Error) },
        { 23, ("Неисправность VLV_F-", PackageTechStatus.Error) },
        { 22, ("Неисправность VLV_F+", PackageTechStatus.Error) },
        { 21, ("Неисправность VLV_Z-", PackageTechStatus.Error) },
        { 20, ("Неисправность VLV_Z+", PackageTechStatus.Error) },
        { 19, ("Неисправность компрессора продувки", PackageTechStatus.Error) },
        { 18, ("Неисправность клапана безопасности", PackageTechStatus.Error) },
        { 17, ("Неисправность генератора потока", PackageTechStatus.Error) },
        { 16, ("Неисправность EEPROM", PackageTechStatus.Error) },
        { 15, ("Ошибка констант генератора потока", PackageTechStatus.Error) },
        { 14, ("Ошибка констант клапана выдоха", PackageTechStatus.Error) },
        { 13, ("Ошибка констант преобразователя «поток-давление»", PackageTechStatus.Error) },
        { 12, ("Ошибка констант датчиков давления", PackageTechStatus.Error) },
        { 11, ("Ошибка питания 12VA", PackageTechStatus.Error) },
        { 10, ("Ошибка питания 2.5VA", PackageTechStatus.Error) },
        { 9,  ("Ошибка питания 5VA", PackageTechStatus.Error) },
        { 8,  ("Ошибка питания 12V_VLV", PackageTechStatus.Error) },
        { 7,  ("Ошибка питания 15VA", PackageTechStatus.Error) },
        { 6,  ("Ошибка питания V_EMV", PackageTechStatus.Error) },
        { 5,  ("Ошибка питания 27V_PWR", PackageTechStatus.Error) },
        { 4,  ("Апноэ", PackageTechStatus.Warning) },
        { 3,  ("Достижение Pmax", PackageTechStatus.Warning) },
        { 2,  ("Окклюзия дыхательного контура", PackageTechStatus.Warning) },
        { 1,  ("Разгерметизация", PackageTechStatus.Warning) },
        { 0,  ("Нет связи с БУ", PackageTechStatus.Error) }
    };
    
    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }
        var bits = BitUtil.ToU64(Data[0], Data[1], Data[2], Data[3], Data[4], Data[5]);

        var numericData = Array.Empty<NumericDataItem>();
        var messages = ParseBits(bits, BitsDefinitions);

        return new PackageData(numericData, messages);
    }
}
