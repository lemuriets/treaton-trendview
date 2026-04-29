using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4C3, "Состояние преобразователя протоколов")]
public class IdStatusCapno2 : BasePackageParsed
{
    public const int Id = 0x4C3;

    public IdStatusCapno2(CanPackage p, string name) : base(p, name) { }

    private static readonly Dictionary<int, BitMessage> StatusBitDefinitions = new()
    {
        [0] = BitMessage.One("Отрицательное значение CO2. Ошибка калибровки нуля или ошибка адаптера", PackageTechStatus.Warning),
        [1] = BitMessage.One("Ошибка адаптера. Загрязнён или изменён тип адаптера без перекалибровки", PackageTechStatus.Warning),
        [2] = BitMessage.One("Обнаружено дыхание", PackageTechStatus.Ok),
        [3] = BitMessage.One("Высокое CO2. Достигнут верхний предел измерения. Данный бит нужно игнорировать", PackageTechStatus.Warning),
        [4] = BitMessage.One("Капнограф не может начать калибровку", PackageTechStatus.Warning),
        [5] = BitMessage.One("Активен режим сна", PackageTechStatus.Ok),
        [6] = BitMessage.One("Апноэ. Превышено время ожидания апноэ", PackageTechStatus.Warning),
        [7] = BitMessage.Both(
            "Капнограф работает в открытом контуре",
            "Капнограф работает в закрытом контуре"),

        [12] = BitMessage.One("Capnostat5 не проинициализирован. Не передано атмосферное давление и параметры газа", PackageTechStatus.Error),

        [21] = BitMessage.One("Аппаратная ошибка", PackageTechStatus.Error),
        [22] = BitMessage.One("Неисправна EEPROM", PackageTechStatus.Error),
        [24] = BitMessage.One("Отсутствует адаптер бокового потока", PackageTechStatus.Error),
        [25] = BitMessage.One("Насос выработал свой ресурс", PackageTechStatus.Error),
        [26] = BitMessage.One("Ошибка пневматики", PackageTechStatus.Error),
        [27] = BitMessage.One("Аппаратная ошибка", PackageTechStatus.Error),
    };

    private static readonly Dictionary<ulong, MessageDefinition> TemperatureStatusDefinitions = new()
    {
        [0] = new("Температура стабильна, модуль готов к работе", PackageTechStatus.Ok),
        [1] = new("Температура ниже требуемой", PackageTechStatus.Warning),
        [2] = new("Температура выше требуемой", PackageTechStatus.Warning),
        [3] = new("Температура нестабильна, идёт прогрев капнографа", PackageTechStatus.Warning),
    };

    private static readonly Dictionary<ulong, MessageDefinition> ZeroCalibrationStatusDefinitions = new()
    {
        [0] = new("Калибровка нуля не требуется", PackageTechStatus.Ok),
        [1] = new("Активна калибровка нуля", PackageTechStatus.Info),
        [2] = new("Необходимо провести калибровку нуля", PackageTechStatus.Warning),
        [3] = new("Ошибка калибровки нуля. Загрязнение адаптера, наличие CO2 или отсутствие адаптера", PackageTechStatus.Error),
    };

    private static readonly Dictionary<ulong, MessageDefinition> CurrentStabilizerStatusDefinitions = new()
    {
        [0] = new("Ток в норме", PackageTechStatus.Ok),
        [1] = new("Ток стабилизируется", PackageTechStatus.Info),
        [2] = new("Ток не в норме. Аппаратная ошибка", PackageTechStatus.Error),
        [3] = new("Ток ограничивается. Аппаратная ошибка", PackageTechStatus.Error),
    };

    private static readonly Dictionary<ulong, MessageDefinition> PriorityMessageDefinitions = new()
    {
        [0] = new("Отсутствие ошибок", PackageTechStatus.Ok),
        [1] = new("Датчик перегрет. Аппаратная ошибка. Температура датчика выше 40 °C", PackageTechStatus.Error),
        [2] = new("Датчик дефектный. Аппаратная ошибка", PackageTechStatus.Error),
        [3] = new("Не задано атмосферное давление. Для работы капнографа необходимо задать атмосферное давление", PackageTechStatus.Warning),
        [4] = new("Капнограф находится в спящем режиме", PackageTechStatus.Info),
        [5] = new("Активна коррекция нуля", PackageTechStatus.Info),
        [6] = new("Активен прогрев датчика", PackageTechStatus.Info),
        [7] = new("Ошибка измерения нуля. Необходимо выполнить калибровку нуля", PackageTechStatus.Warning),
        [8] = new("Высокое значение CO2", PackageTechStatus.Warning),
        [9] = new("Ошибка адаптера. Обычно вызывается, когда адаптер удалён из модуля капнографа", PackageTechStatus.Warning),
        [10] = new("Высокое давление в линии отбора пробы", PackageTechStatus.Warning),
    };

    private static readonly Dictionary<ulong, MessageDefinition> PresenceDefinitions = new()
    {
        [0x00] = new("Капнограф подключен к преобразователю протокола", PackageTechStatus.Ok),
        [0x14] = new("Капнограф не подключен к преобразователю протокола", PackageTechStatus.Warning),
    };

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }

        var span = Data.Span;

        var statusBits = BitUtil.ToU32(span[0], span[1], span[2], span[3]);
        var priority = span[4];
        var presence = span[5];
        var atmosphericPressure = BitUtil.ToU16(span[6], span[7]);

        var messages = new List<string>();
        var numericData = new List<NumericDataItem>
        {
            new("Номер наиболее приоритетного сообщения", priority),
            new("Наличие капнографа", presence),
            new("Атмосферное давление [мм рт.ст.]", atmosphericPressure),
        };

        messages.AddRange(ParseValueMessageAndUpdateStatus(presence, PresenceDefinitions));

        if (presence == 0x00)
        {
            messages.AddRange(ParseBitsAndUpdateStatus(statusBits, StatusBitDefinitions));

            messages.AddRange(ParseValueMessageAndUpdateStatus(
                BitUtil.ExtractBits(statusBits, 8, 9),
                TemperatureStatusDefinitions));

            messages.AddRange(ParseValueMessageAndUpdateStatus(
                BitUtil.ExtractBits(statusBits, 10, 11),
                ZeroCalibrationStatusDefinitions));

            messages.AddRange(ParseValueMessageAndUpdateStatus(
                BitUtil.ExtractBits(statusBits, 13, 14),
                CurrentStabilizerStatusDefinitions));

            messages.AddRange(ParseValueMessageAndUpdateStatus(priority, PriorityMessageDefinitions));
        }

        return new PackageData(numericData, messages);
    }
}