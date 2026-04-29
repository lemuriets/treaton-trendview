using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4E1, "Состояния модуля пульсометрии Treaton")]
public class IdStatusSpo : BasePackageParsed
{
    public const int Id = 0x4E1;

    private static readonly Dictionary<ulong, MessageDefinition> SensorStatusDefinitions = new()
    {
        [0] = new("Норма, нет сообщения", PackageTechStatus.Ok),
        [1] = new("Модуль отключен", PackageTechStatus.Info),
        [2] = new("Модуль неисправен", PackageTechStatus.Error),
        [3] = new("Датчик отсоединен", PackageTechStatus.Warning),
        [4] = new("Датчик неисправен", PackageTechStatus.Error),
        [5] = new("Датчик сброшен", PackageTechStatus.Info),
        [6] = new("Слабый сигнал датчика ФПГ", PackageTechStatus.Warning),
        [7] = new("Деградация датчика", PackageTechStatus.Warning),
    };

    public IdStatusSpo(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 6)
        {
            return null;
        }
        var span = Data.Span;
        
        var sensorStatus = BitUtil.ExtractBits(span[5], 0, 3);
        // 0.1%
        var pleth = BitUtil.ToU16(span[0], span[1]);
        // уд/мин
        var pulseRate = BitUtil.ToU16(span[2], span[3]);
        var spO2 = span[4];
        
        var numericData = new NumericDataItem[]
        {
            new("pleth", pleth),
            new("pulseRate", pulseRate),
            new("spO2", spO2)
        };;
        var messages = ParseValueMessageAndUpdateStatus(sensorStatus, SensorStatusDefinitions);

        return new PackageData(numericData, messages);
    }
}
