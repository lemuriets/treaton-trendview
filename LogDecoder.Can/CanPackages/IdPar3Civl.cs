using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4A5, "Параметры вентиляции 3")]
public class IdPar3Civl : BasePackageParsed
{
    public IdPar3Civl(CanPackage p, string name) : base(p, name) { }

    public const int Id = 0x4A5;

    private static readonly Dictionary<ulong, MessageDefinition> BytesDefinitions = new()
    {
        { 0, new ("вкл компенсацию утечки") },
        { 1, new ("вкл функцию \"свободное дыхание\"") },
        { 2, new ("неонатальный/детский режим ИВЛ") },
    };

    public override PackageData? ParseData()
    {
        if (Data.Length < 7)
        {
            return null;
        }

        var span = Data.Span;

        var riseSpeed = BitUtil.ToU16(span[0], span[1]);
        var waveByte = span[2];
        var supportFlow = span[3];
        var disconnectSensitivity = Math.Min(span[4], (byte)100);
        var deltaPeep = span[5];
        var flags = span[6];

        var numericData = new List<NumericDataItem>
        {
            new("Скорость нарастания давления [ммH2O/с]", riseSpeed),
            new("Byte 6", waveByte),
            new("Поток поддержки [л/мин]", supportFlow),
            new("Чувствительность к дисконнекции [%]", disconnectSensitivity),
            new("Приращение deltaPEEP", deltaPeep),
        };

        var messages = ParseValueMessageAndUpdateStatus(flags, BytesDefinitions);

        return new PackageData(numericData, messages);
    }
}