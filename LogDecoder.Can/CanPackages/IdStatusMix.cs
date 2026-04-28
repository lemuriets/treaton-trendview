using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x442, "Состояние платы смесителя газов")]
public class IdStatusMix : BasePackageParsed
{
    public const int Id = 0x442;

    private static readonly Dictionary<int,(string,PackageTechStatus)> BitsDefinitions = new()
    {
        { 0, ("Рестарт", PackageTechStatus.Info) },
        { 11, ("Неисправность (загрязнение) НЕРА-фильтра", PackageTechStatus.Warning) },
        { 13, ("Неисправность CAN", PackageTechStatus.Warning) },
        { 15, ("Неисправность источника питания +12VA", PackageTechStatus.Warning) },
        { 18, ("Ошибка констант датчика потока в магистрали воздуха", PackageTechStatus.Warning) },
        { 19, ("Ошибка констант датчика потока в магистрали кислорода", PackageTechStatus.Warning) },
        { 20, ("Ошибка констант датчика давления в камере смешения", PackageTechStatus.Warning) },
        { 21, ("Напряжение питания выше 27V (превышает 30 V)", PackageTechStatus.Warning) },
        { 22, ("Напряжение питания ниже 27V (ниже 20 V)", PackageTechStatus.Warning) },
        { 23, ("Отсутствует связь с БУ", PackageTechStatus.Error) },
        { 24, ("Отсутствует связь с КИВЛ", PackageTechStatus.Error) },
        { 25, ("неисправен пропорциональный клапан в магистрали О2 (не используется)", PackageTechStatus.Error) },
        { 26, ("Неисправен АЦП AD7738", PackageTechStatus.Error) },
        { 27, ("Неисправен ИОН", PackageTechStatus.Error) },
        { 28, ("неисправен датчик потока в магистрали воздуха (выходное напряжение вне допустимого предела или не в пределах напряжение смещения нуля в режиме калибровки)", PackageTechStatus.Error) },
        { 29, ("неисправен датчик потока в магистрали О2 (выходное напряжение вне допустимого предела или не в пределах напряжение смещения нуля в режиме калибровки)", PackageTechStatus.Error) },
        { 30, ("Неисправен датчик абсолютного давления", PackageTechStatus.Error) },
        { 31, ("Неисправен датчик давления кислорода", PackageTechStatus.Error) },
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
            new("atmPressure", atmospherePressure),
            new("mode", activeMode)
        };
        var messages = ParseBitsAndUpdateStatus(statusBits, BitsDefinitions);

        return new PackageData(numericData, messages);
    }
}

