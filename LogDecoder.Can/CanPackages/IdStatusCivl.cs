using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4A8, "Состояние КИВЛ")]
public class IdStatusCivl : BasePackageParsed
{
    public IdStatusCivl(CanPackage p, string name) : base(p, name) { }
    
    public const int Id = 0x4A8;

    private static readonly Dictionary<int, (string, PackageTechStatus)> BitsDefinitions = new()
    {
        { 47, ("Окклюзия ЭТ-трубки", PackageTechStatus.Warning) },
        { 46, ("Окклюзия клапана", PackageTechStatus.Warning) },
        { 45, ("Окклюзия контура", PackageTechStatus.Warning) },
        { 44, ("Неисправность небулайзера", PackageTechStatus.Error) },
        { 43, ("AutoLeak", PackageTechStatus.Info) },
        { 42, ("Признак подключения внешнего датчика потока", PackageTechStatus.Info) },
        { 41, ("Признак подключения внешнего модуля потока", PackageTechStatus.Info) },
        { 40, ("Признак возникновения рестарта", PackageTechStatus.Info) },
        { 39, ("Ошибка автоматической калибровки датчика кислорода", PackageTechStatus.Warning) },
        { 38, ("Признак работы алгоритма автоматической калибровки датчика кислорода", PackageTechStatus.Info) },
        { 37, ("Признак манёвра раскрытия альвеол", PackageTechStatus.Info) },
        { 36, ("Сбой в памяти начального напряжения на датчике кислорода", PackageTechStatus.Error) },
        { 35, ("Ошибка калибровочного коэффициента датчика кислорода", PackageTechStatus.Error) },
        { 34, ("Неисправность ГП (большая потребляемая мощность)", PackageTechStatus.Error) },
        { 33, ("Неисправность ИОН для AD7738", PackageTechStatus.Error) },
        { 32, ("Ошибка передачи по CAN", PackageTechStatus.Error) },
        { 31, ("Признак работающего небулайзера", PackageTechStatus.Info) },
        { 30, ("Отсутствие связи с СГ", PackageTechStatus.Error) },
        { 29, ("Неисправность дифференциального датчика давления для датчика потока в электромагнитном клапане выдоха", PackageTechStatus.Error) },
        { 28, ("Неисправность датчика давления в магистрали выдоха", PackageTechStatus.Error) },
        { 27, ("Неисправность датчика давления в магистрали вдоха", PackageTechStatus.Error) },
        { 26, ("Неисправность АЦП AD7738", PackageTechStatus.Error) },
        { 25, ("Неисправность датчика кислорода", PackageTechStatus.Error) },
        { 24, ("Неисправность клапана выдоха", PackageTechStatus.Error) },
        { 23, ("Неисправность VLV_F-", PackageTechStatus.Error) },
        { 22, ("Неисправность VLV_F+", PackageTechStatus.Error) },
        { 21, ("Неисправность VLV_Z-", PackageTechStatus.Error) },
        { 20, ("Неисправность VLV_Z+", PackageTechStatus.Error) },
        { 19, ("Неисправность компрессора продувки", PackageTechStatus.Error) },
        { 18, ("Неисправность клапана безопасности", PackageTechStatus.Error) },
        { 17, ("Неисправность ГП (без указания причины)", PackageTechStatus.Error) },
        { 16, ("Неисправность EEPROM", PackageTechStatus.Error) },
        { 15, ("Ошибка констант генератора потока", PackageTechStatus.Error) },
        { 14, ("Ошибка констант клапана выдоха", PackageTechStatus.Error) },
        { 13, ("Ошибка констант преобразователя \"поток-давление\"", PackageTechStatus.Error) },
        { 12, ("Ошибка констант датчиков давления", PackageTechStatus.Error) },
        { 11, ("Ошибка напряжения 12VA", PackageTechStatus.Error) },
        { 10, ("Ошибка напряжения 2.5VA", PackageTechStatus.Error) },
        { 9,  ("Ошибка напряжения 5VA", PackageTechStatus.Error) },
        { 8,  ("Ошибка напряжения 12V_VLV", PackageTechStatus.Error) },
        { 7,  ("Ошибка напряжения 15VA", PackageTechStatus.Error) },
        { 6,  ("Ошибка напряжения V_EMV", PackageTechStatus.Error) },
        { 5,  ("Ошибка напряжения 27V_PWR", PackageTechStatus.Error) },
        { 4,  ("Апноэ", PackageTechStatus.Warning) },
        { 3,  ("Достижение Pmax", PackageTechStatus.Warning) },
        { 2,  ("Окклюзия дыхательного контура", PackageTechStatus.Warning) },
        { 1,  ("Разгерметизация", PackageTechStatus.Warning) },
        { 0,  ("Отсутствие связи с БУ", PackageTechStatus.Error) },
    };
    
    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }
        var span = Data.Span;

        var bits = BitUtil.ToU64(span.Slice(0, 6));

        var workModeCode = span[6];
        var o2Percent = span[7];
        var numericData = new NumericDataItem[]
        {
            new("код текущего режима работы", workModeCode),
            new("текущий %O2, измеренный датчиком кислорода", o2Percent),
        };
        var messages = ParseBitsAndUpdateStatus(bits, BitsDefinitions);

        return new PackageData(numericData, messages);
    }
}
