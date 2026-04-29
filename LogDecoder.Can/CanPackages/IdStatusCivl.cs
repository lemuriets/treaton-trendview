using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4A8, "Состояние КИВЛ")]
public class IdStatusCivl : BasePackageParsed
{
    public IdStatusCivl(CanPackage p, string name) : base(p, name) { }
    
    public const int Id = 0x4A8;

    private static readonly Dictionary<int, BitMessage> BitsDefinitions = new()
    {
        { 47, BitMessage.One("Окклюзия ЭТ-трубки", PackageTechStatus.Warning) },
        { 46, BitMessage.One("Окклюзия клапана", PackageTechStatus.Warning) },
        { 45, BitMessage.One("Окклюзия контура", PackageTechStatus.Warning) },
        { 44, BitMessage.One("Неисправность небулайзера", PackageTechStatus.Error) },
        { 43, BitMessage.One("AutoLeak") },
        { 42, BitMessage.One("Признак подключения внешнего датчика потока") },
        { 41, BitMessage.One("Признак подключения внешнего модуля потока") },
        { 40, BitMessage.One("Признак возникновения рестарта") },
        { 39, BitMessage.One("Ошибка автоматической калибровки датчика кислорода", PackageTechStatus.Warning) },
        { 38, BitMessage.One("Признак работы алгоритма автоматической калибровки датчика кислорода") },
        { 37, BitMessage.One("Признак манёвра раскрытия альвеол") },
        { 36, BitMessage.One("Сбой в памяти начального напряжения на датчике кислорода", PackageTechStatus.Error) },
        { 35, BitMessage.One("Ошибка калибровочного коэффициента датчика кислорода", PackageTechStatus.Error) },
        { 34, BitMessage.One("Неисправность ГП (большая потребляемая мощность)", PackageTechStatus.Error) },
        { 33, BitMessage.One("Неисправность ИОН для AD7738", PackageTechStatus.Error) },
        { 32, BitMessage.One("Ошибка передачи по CAN", PackageTechStatus.Error) },
        { 31, BitMessage.One("Признак работающего небулайзера") },
        { 30, BitMessage.One("Отсутствие связи с СГ", PackageTechStatus.Error) },
        { 29, BitMessage.One("Неисправность дифференциального датчика давления для датчика потока в электромагнитном клапане выдоха", PackageTechStatus.Error) },
        { 28, BitMessage.One("Неисправность датчика давления в магистрали выдоха", PackageTechStatus.Error) },
        { 27, BitMessage.One("Неисправность датчика давления в магистрали вдоха", PackageTechStatus.Error) },
        { 26, BitMessage.One("Неисправность АЦП AD7738", PackageTechStatus.Error) },
        { 25, BitMessage.One("Неисправность датчика кислорода", PackageTechStatus.Error) },
        { 24, BitMessage.One("Неисправность клапана выдоха", PackageTechStatus.Error) },
        { 23, BitMessage.One("Неисправность VLV_F-", PackageTechStatus.Error) },
        { 22, BitMessage.One("Неисправность VLV_F+", PackageTechStatus.Error) },
        { 21, BitMessage.One("Неисправность VLV_Z-", PackageTechStatus.Error) },
        { 20, BitMessage.One("Неисправность VLV_Z+", PackageTechStatus.Error) },
        { 19, BitMessage.One("Неисправность компрессора продувки", PackageTechStatus.Error) },
        { 18, BitMessage.One("Неисправность клапана безопасности", PackageTechStatus.Error) },
        { 17, BitMessage.One("Неисправность ГП (без указания причины)", PackageTechStatus.Error) },
        { 16, BitMessage.One("Неисправность EEPROM", PackageTechStatus.Error) },
        { 15, BitMessage.One("Ошибка констант генератора потока", PackageTechStatus.Error) },
        { 14, BitMessage.One("Ошибка констант клапана выдоха", PackageTechStatus.Error) },
        { 13, BitMessage.One("Ошибка констант преобразователя \"поток-давление\"", PackageTechStatus.Error) },
        { 12, BitMessage.One("Ошибка констант датчиков давления", PackageTechStatus.Error) },
        { 11, BitMessage.One("Ошибка напряжения 12VA", PackageTechStatus.Error) },
        { 10, BitMessage.One("Ошибка напряжения 2.5VA", PackageTechStatus.Error) },
        { 9,  BitMessage.One("Ошибка напряжения 5VA", PackageTechStatus.Error) },
        { 8,  BitMessage.One("Ошибка напряжения 12V_VLV", PackageTechStatus.Error) },
        { 7,  BitMessage.One("Ошибка напряжения 15VA", PackageTechStatus.Error) },
        { 6,  BitMessage.One("Ошибка напряжения V_EMV", PackageTechStatus.Error) },
        { 5,  BitMessage.One("Ошибка напряжения 27V_PWR", PackageTechStatus.Error) },
        { 4,  BitMessage.One("Апноэ", PackageTechStatus.Warning) },
        { 3,  BitMessage.One("Достижение Pmax", PackageTechStatus.Warning) },
        { 2,  BitMessage.One("Окклюзия дыхательного контура", PackageTechStatus.Warning) },
        { 1,  BitMessage.One("Разгерметизация", PackageTechStatus.Warning) },
        { 0,  BitMessage.One("Отсутствие связи с БУ", PackageTechStatus.Error) },
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
