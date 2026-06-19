using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x582, "Состояние модуля пульсоксиметрии Masimo 2")]
public class IdStatusSpo_v2_2 : BasePackageParsed
{
    public const int Id = 0x582;

    public IdStatusSpo_v2_2(CanPackage package, string name) : base(package, name) { }

    private static readonly Dictionary<int, BitMessage> Byte6BitDefinitions = new()
    {
        [0] = BitMessage.Both(
            "Измерение PVI не поддерживается",
            "PVI поддержан"),

        [1] = BitMessage.One("PVI Exception: Invalid PVI", PackageTechStatus.Warning),
        [2] = BitMessage.One("PVI Exception: StartupState"),
        [3] = BitMessage.One("PVI Exception: Low PVI Confidence", PackageTechStatus.Warning),
    };

    private static readonly Dictionary<ulong, MessageDefinition> PviExceptionDefinitions = new()
    {
        [0] = new("PVI Exception: всё нормально", PackageTechStatus.Ok),
        [1] = new("PVI Exception: Invalid PVI", PackageTechStatus.Warning),
        [2] = new("PVI Exception: StartupState"),
        [3] = new("PVI Exception: Low PVI Confidence", PackageTechStatus.Warning),
    };

    private static readonly Dictionary<ulong, MessageDefinition> SensorFailureDefinitions = new()
    {
        [0] = new("Детализация сообщения датчика: No Failure", PackageTechStatus.Ok),
        [1] = new("Детализация сообщения датчика: Open LEDs", PackageTechStatus.Error),
        [2] = new("Детализация сообщения датчика: Shorted LEDs", PackageTechStatus.Error),
        [3] = new("Детализация сообщения датчика: Shorted Detector", PackageTechStatus.Error),
        [4] = new("Детализация сообщения датчика: Bad Sensor ID Offset", PackageTechStatus.Error),
        [5] = new("Детализация сообщения датчика: Pro Cal Failure", PackageTechStatus.Error),
        [6] = new("Детализация сообщения датчика: Memory Failure", PackageTechStatus.Error),
        [7] = new("Детализация сообщения датчика: reserved"),
        [8] = new("Детализация сообщения датчика: reserved"),
    };

    private static readonly Dictionary<int, BitMessage> Byte7BitDefinitions = new()
    {
        [0] = BitMessage.Both(
            "Качество Exceptions 2: значение недостоверное, нужно вывести прочерки",
            "Качество Exceptions 2: значение OK"),

        [1] = BitMessage.Both(
            "Качество Diagnostic Failure Codes: значение недостоверное, нужно вывести прочерки",
            "Качество Diagnostic Failure Codes: значение OK"),

        [2] = BitMessage.Both(
            "Качество Board Failure Codes: значение недостоверное, нужно вывести прочерки",
            "Качество Board Failure Codes: значение OK"),

        [3] = BitMessage.Both(
            "Качество PVI Exception: значение недостоверное, нужно вывести прочерки",
            "Качество PVI Exception: значение OK"),

        [4] = BitMessage.Both(
            "Качество SFail: значение недостоверное, нужно вывести прочерки",
            "Качество SFail: значение OK"),

        [5] = BitMessage.Both(
            "Качество Exceptions 3 seq 22: значение недостоверное, нужно вывести прочерки",
            "Качество Exceptions 3 seq 22: значение OK"),

        [6] = BitMessage.Both(
            "Качество Exceptions 3 seq 23: значение недостоверное, нужно вывести прочерки",
            "Качество Exceptions 3 seq 23: значение OK"),

        [7] = BitMessage.Both(
            "Режим демо выключен",
            "Режим демо включен"),
    };

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }

        var span = Data.Span;

        var exc2 = BitUtil.ToU16(span[0], span[1]);
        var dfc = BitUtil.ToU16(span[2], span[3]);
        var bfc = BitUtil.ToU16(span[4], span[5]);

        var byte6 = span[6];
        var byte7 = span[7];

        var pviException = BitUtil.ExtractBits(byte6, 1, 3);
        var sensorFailure = BitUtil.ExtractBits(byte6, 4, 7);

        var numericData = new List<NumericDataItem>
        {
            new("Exceptions 2", exc2),
            new("Diagnostic Failure Codes", dfc),
            new("Board Failure Codes", bfc),
            new("PVI Exception", pviException),
            new("SFail", sensorFailure),
        };

        var messages = ParseBitsAndUpdateStatus(byte6, Byte6BitDefinitions);

        messages.AddRange(ParseValueMessageAndUpdateStatus(
            pviException,
            PviExceptionDefinitions));

        messages.AddRange(ParseValueMessageAndUpdateStatus(
            sensorFailure,
            SensorFailureDefinitions));

        messages.AddRange(ParseBitsAndUpdateStatus(byte7, Byte7BitDefinitions));

        return new PackageData(numericData, messages);
    }
}