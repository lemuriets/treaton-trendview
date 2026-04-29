using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x581, "Состояние модуля пульсоксиметрии Masimo 1")]
public class IdStatusSpo_v2_1 : BasePackageParsed
{
    public const int Id = 0x581;

    public IdStatusSpo_v2_1(CanPackage package, string name) : base(package, name) { }

    private static readonly Dictionary<ulong, MessageDefinition> SensorStateDefinitions = new()
    {
        [0] = new("Норма, нет сообщения", PackageTechStatus.Ok),
        [1] = new("Датчик не подключен", PackageTechStatus.Warning),
        [2] = new("Датчик сброшен", PackageTechStatus.Info),
        [3] = new("Датчик неисправен", PackageTechStatus.Error),
        [4] = new("Деградация датчика", PackageTechStatus.Warning),
        [5] = new("Слабый сигнал ФПГ", PackageTechStatus.Warning),
        [6] = new("Установка соединения с модулем", PackageTechStatus.Info),
        [7] = new("Модуль отсутствует", PackageTechStatus.Error),
        [8] = new("Высокий уровень помех", PackageTechStatus.Warning),
        [9] = new("Калибровка датчика", PackageTechStatus.Info),
        [10] = new("Поиск пульса", PackageTechStatus.Info),
        [11] = new("Несовместимый датчик", PackageTechStatus.Error),
        [12] = new("Модуль неисправен", PackageTechStatus.Error),
        [13] = new("Измерения недостоверны", PackageTechStatus.Warning),
    };

    private static readonly Dictionary<ulong, MessageDefinition> SensorTypeDefinitions = new()
    {
        [0] = new("Тип датчика: датчик не подключен", PackageTechStatus.Warning),
        [1] = new("Тип датчика: Masimo LNOP Sensor", PackageTechStatus.Ok),
        [2] = new("Тип датчика: резерв", PackageTechStatus.Info),
        [3] = new("Тип датчика: резерв", PackageTechStatus.Info),
        [4] = new("Тип датчика: Unknown Sensor", PackageTechStatus.Warning),
        [5] = new("Тип датчика: Hi Fi (Trauma or Newborn) Sensor", PackageTechStatus.Ok),
        [6] = new("Тип датчика: резерв", PackageTechStatus.Info),
        [7] = new("Тип датчика: резерв", PackageTechStatus.Info),
        [8] = new("Тип датчика: резерв", PackageTechStatus.Info),
        [9] = new("Тип датчика: резерв", PackageTechStatus.Info),
        [10] = new("Тип датчика: резерв", PackageTechStatus.Info),
        [11] = new("Тип датчика: резерв", PackageTechStatus.Info),
        [12] = new("Тип датчика: резерв", PackageTechStatus.Info),
        [13] = new("Тип датчика: резерв", PackageTechStatus.Info),
        [14] = new("Тип датчика: резерв", PackageTechStatus.Info),
        [15] = new("Тип датчика: резерв", PackageTechStatus.Info),
    };

    private static readonly Dictionary<int, BitMessage> QualityBitDefinitions = new()
    {
        [0] = BitMessage.Both(
            "Качество PI: значение недостоверное, нужно вывести прочерки",
            "Качество PI: значение OK"),

        [1] = BitMessage.Both(
            "Качество PR: значение недостоверное, нужно вывести прочерки",
            "Качество PR: значение OK"),

        [2] = BitMessage.Both(
            "Качество SpO2: значение недостоверное, нужно вывести прочерки",
            "Качество SpO2: значение OK"),

        [3] = BitMessage.Both(
            "Качество PVI: значение недостоверное, нужно вывести прочерки",
            "Качество PVI: значение OK"),

        [4] = BitMessage.Both(
            "Качество SType: значение недостоверное",
            "Качество SType: значение OK"),
    };

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }

        var span = Data.Span;

        var filling = BitUtil.ToU16(span[0], span[1]) * 0.001;
        var pulse = BitUtil.ToU16(span[2], span[3]);
        var spo2 = span[4];
        var pvi = span[5];

        var sensorStatus = span[6];
        var sensorState = BitUtil.ExtractBits(sensorStatus, 0, 3);
        var sensorType = BitUtil.ExtractBits(sensorStatus, 4, 7);

        var qualityStatus = span[7];
        var workMode = BitUtil.ExtractBits(qualityStatus, 6, 7);

        var numericData = new List<NumericDataItem>
        {
            new("Наполнение [0.001%]", filling),
            new("Частота пульса [уд/мин]", pulse),
            new("Сатурация [%]", spo2),
            new("PVI [%]", pvi),
            new("Режим работы блока", workMode),
        };

        var messages = new List<string>();

        messages.AddRange(ParseValueMessageAndUpdateStatus(sensorState, SensorStateDefinitions));
        messages.AddRange(ParseValueMessageAndUpdateStatus(sensorType, SensorTypeDefinitions));
        messages.AddRange(ParseBitsAndUpdateStatus(qualityStatus, QualityBitDefinitions));

        return new PackageData(numericData, messages);
    }
}