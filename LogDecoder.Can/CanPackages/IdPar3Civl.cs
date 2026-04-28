using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4A5, "Параметры вентиляции 3")]
public class IdPar3Civl : BasePackageParsed
{
    public IdPar3Civl(CanPackage p, string name) : base(p, name) { }

    public const int Id = 0x4A5;

    private static readonly Dictionary<int, (string, PackageTechStatus)> BitDefinitions = new()
    {
        { 0, ("вкл компенсацию утечки", PackageTechStatus.Info) },
        { 1, ("вкл функцию \"свободное дыхание\"", PackageTechStatus.Info) },
        { 2, ("неонатальный/детский режим ИВЛ", PackageTechStatus.Info) },
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

        var waveDiv = waveByte is >= 1 and <= 33 ? waveByte : (byte)33;
        var waveFreq = 500.0 / waveDiv;
        if (waveFreq < 15.0)
        {
            waveFreq = 15.0;
        }

        var numericData = new List<NumericDataItem>
        {
            new("Скорость нарастания давления [ммH2O/с]", riseSpeed),
            new("Частота выдачи ID_WAVE_CIVL [Гц]", waveFreq),
            new("Поток поддержки [л/мин]", supportFlow),
            new("Чувствительность к дисконнекции [%]", disconnectSensitivity),
            new("Приращение deltaPEEP", deltaPeep),
        };

        var messages = ParseBitsAndUpdateStatus(flags, BitDefinitions).ToList();

        return new PackageData(numericData, messages);
    }
}