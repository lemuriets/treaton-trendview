using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x510, "Статус модуля 2")]
public class IdStatusCivl2 : BasePackageParsed
{
    public IdStatusCivl2(CanPackage p, string name) : base(p, name) { }

    public const int Id = 0x510;

    private static readonly Dictionary<int, (string, PackageTechStatus)> BitsDefinitions = new()
    {
        { 0, ("Неисправность датчика потока Jet", PackageTechStatus.Error) },
        { 1, ("Неисправность клапана безопасности Jet", PackageTechStatus.Error) },
        { 2, ("Неисправность регулятора ШИМ Jet", PackageTechStatus.Error) },
        { 3, ("Неисправность АЦП1", PackageTechStatus.Error) },
        { 4, ("Неисправность модуля респираторной механики", PackageTechStatus.Error) },
        { 5, ("Превышен предел давления nCPAP", PackageTechStatus.Warning) },
        { 6, ("Неисправность датчика давления Aux1", PackageTechStatus.Error) },
        { 7, ("Неисправность системного напряжения (27V)", PackageTechStatus.Error) },
        { 8, ("Неисправность датчика температуры и атмосферного давления на плате КИВЛ", PackageTechStatus.Error) },
        { 9, ("Заданный объём вдоха (Vt) недостижим", PackageTechStatus.Warning) },
        { 10, ("Значение Paux не соответствует установленному значению", PackageTechStatus.Warning) },
        { 11, ("Достигнуто давление 50 см.вод.ст.", PackageTechStatus.Warning) },
    };

    public override PackageData? ParseData()
    {
        if (Data.Length < 2)
        {
            return null;
        }

        var span = Data.Span;
        var statusBits = BitUtil.ToU16(span[0], span[1]);

        var messages = ParseBitsAndUpdateStatus(statusBits, BitsDefinitions);
        var numericData = Array.Empty<NumericDataItem>();

        return new PackageData(numericData, messages);
    }
}