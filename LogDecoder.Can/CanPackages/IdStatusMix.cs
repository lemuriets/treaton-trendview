using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x442, "Состояние платы СГ")]
public class IdStatusMix : BasePackageParsed
{
    public const int Id = 0x442;

    private static readonly Dictionary<int,(string,PackageTechStatus)> BitsDefinitions = new()
    {
        { 11, ("Неисправность (загрязнение) НЕРА-фильтра", PackageTechStatus.Warning) },
        { 13, ("Неисправность CAN", PackageTechStatus.Warning) },
        { 15, ("Неисправность источника питания +12VA", PackageTechStatus.Warning) },
        { 18, ("Ошибка констант датчика потока в магистрали воздуха", PackageTechStatus.Warning) },
        { 19, ("Ошибка констант датчика потока в магистрали кислорода", PackageTechStatus.Warning) },
        { 20, ("Ошибка констант датчика давления в камере смешения", PackageTechStatus.Warning) },
        { 23, ("Отсутствует связь с БУ", PackageTechStatus.Error) },
        { 24, ("Отсутствует связь с КИВЛ", PackageTechStatus.Error) },
        { 26, ("Неисправен АЦП AD7738", PackageTechStatus.Error) },
        { 27, ("Неисправен ИОН", PackageTechStatus.Error) },
        { 29, ("Неисправен датчик потока кислорода", PackageTechStatus.Error) },
        { 28, ("Неисправен датчик потока воздуха", PackageTechStatus.Error) },
        { 31, ("Неисправен датчик давления кислорода", PackageTechStatus.Error) },
        { 30, ("Неисправен датчик абсолютного давления", PackageTechStatus.Error) },
    };

    public IdStatusMix(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }
        var span = Data.Span;

        var statusBits = BitUtil.ToU32(span[4], span[5], span[6], span[7]);

        var oxygenPressure = span[0] * 0.1;
        var atmospherePressure = BitUtil.ToU16(span[1], span[2]);
        var activeMode = span[3];
        
        var numericData = new NumericDataItem[]
        {
            new("oxygenPressure", oxygenPressure),
            new("atmospherePressure", atmospherePressure),
            new("activeMode", activeMode)
        };
        var messages = ParseBits(statusBits, BitsDefinitions);

        return new PackageData(numericData, messages);
    }
}

