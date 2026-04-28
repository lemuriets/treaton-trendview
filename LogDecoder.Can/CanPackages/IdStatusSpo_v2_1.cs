using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x581, "Состояние модуля пульсометрии Masimo 1")]
public class IdStatusSpo_v2_1 : BasePackageParsed
{
    public IdStatusSpo_v2_1(CanPackage p, string name) : base(p, name) { }

    public const int Id = 0x581;

    private static readonly Dictionary<int, string> SensorStateDefinitions = new()
    {
        { 0, "норма - нет сообщения" },
        { 1, "датчик не подключен" },
        { 2, "датчик сброшен" },
        { 3, "датчик неисправен" },
        { 4, "деградация датчика" },
        { 5, "слабый сигнал ФПГ" },
        { 6, "установка соединения с модулем" },
        { 7, "модуль отсутствует" },
        { 8, "высокий уровень помех" },
        { 9, "калибровка датчика" },
        { 10, "поиск пульса" },
        { 11, "несовместимый датчик" },
        { 12, "модуль неисправен" },
        { 13, "измерения недостоверны" },
    };

    private static readonly Dictionary<int, string> SensorTypeDefinitions = new()
    {
        { 0, "датчик не подключен" },
        { 1, "Masimo LNOP Sensor" },
        { 4, "Unknown Sensor" },
        { 5, "Hi Fi (Trauma or Newborn) Sensor" },
    };

    private static readonly Dictionary<int, (string, PackageTechStatus)> QualityBitsDefinitions = new()
    {
        { 0, ("качество PI: значение OK", PackageTechStatus.Ok) },
        { 1, ("качество PR: значение OK", PackageTechStatus.Ok) },
        { 2, ("качество SpO2: значение OK", PackageTechStatus.Ok) },
        { 3, ("качество PVI: значение OK", PackageTechStatus.Ok) },
        { 4, ("качество SType: значение OK", PackageTechStatus.Ok) },
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
        var sensorState = sensorStatus & 0x0F;
        var sensorType = (sensorStatus >> 4) & 0x0F;

        var qualityByte = span[7];
        var mode = (qualityByte >> 6) & 0x03;

        var numericData = new List<NumericDataItem>
        {
            new("Наполнение [0.001%]", filling),
            new("Частота пульса [уд/мин]", pulse),
            new("Сатурация [%]", spo2),
            new("PVI [%]", pvi),
            new("Тип датчика (SType)", sensorType),
            new("Режим работы блока", mode),
        };

        var messages = new List<string>();

        if (SensorStateDefinitions.TryGetValue(sensorState, out var sensorStateMessage) && sensorState != 0)
        {
            messages.Add(sensorStateMessage);
        }

        if (SensorTypeDefinitions.TryGetValue(sensorType, out var sensorTypeMessage))
        {
            messages.Add($"Тип датчика: {sensorTypeMessage}");
        }
        else
        {
            messages.Add($"Тип датчика: резерв ({sensorType})");
        }

        messages.AddRange(ParseBitsAndUpdateStatus(qualityByte, QualityBitsDefinitions));

        if (((qualityByte >> 0) & 1) == 0)
        {
            messages.Add("качество PI: значение недостоверное");
        }

        return new PackageData(numericData, messages);
    }
}