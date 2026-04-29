using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x510, "Статус модуля 2")]
public class IdStatusCivl2 : BasePackageParsed
{
    public IdStatusCivl2(CanPackage p, string name) : base(p, name) { }

    public const int Id = 0x510;

    private static readonly Dictionary<int, BitMessage> BitsDefinitions = new()
    {
        { 0, BitMessage.One("Неисправность датчика потока Jet", PackageTechStatus.Error) },
        { 1, BitMessage.One("Неисправность клапана безопасности Jet", PackageTechStatus.Error) },
        { 2, BitMessage.One("Неисправность регулятора ШИМ Jet", PackageTechStatus.Error) },
        { 3, BitMessage.One("Неисправность АЦП1", PackageTechStatus.Error) },
        { 4, BitMessage.One("Неисправность модуля респираторной механики", PackageTechStatus.Error) },
        { 5, BitMessage.One("Превышен предел давления nCPAP", PackageTechStatus.Warning) },
        { 6, BitMessage.One("Неисправность датчика давления Aux1", PackageTechStatus.Error) },
        { 7, BitMessage.One("Неисправность системного напряжения (27V)", PackageTechStatus.Error) },
        { 8, BitMessage.One("Неисправность датчика температуры и атмосферного давления на плате КИВЛ", PackageTechStatus.Error) },
        { 9, BitMessage.One("Заданный объём вдоха (Vt) недостижим", PackageTechStatus.Warning) },
        { 10, BitMessage.One("Значение Paux не соответствует установленному значению", PackageTechStatus.Warning) },
        { 11, BitMessage.One("Достигнуто давление 50 см.вод.ст.", PackageTechStatus.Warning) },
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