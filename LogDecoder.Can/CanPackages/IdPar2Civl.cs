using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4A4, "Параметры вентиляции 2")]
public class IdPar2Civl : BasePackageParsed
{
    public IdPar2Civl(CanPackage p, string name) : base(p, name) { }

    public const int Id = 0x4A4;

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }

        var span = Data.Span;

        var volume = BitUtil.ToU16(span[0], span[1]); // мл
        var plateau = span[2]; // %
        var ps = span[3]; // см вод. ст
        var pmax = span[4]; // см вод. ст
        var pt = span[5]; // см вод. ст
        var triggerWindow = span[6]; // %
        var expTrigger = span[7]; // %

        var numericData = new List<NumericDataItem>
        {
            new("Объём вдоха [мл]", volume),
            new("Длительность плато [%]", plateau),
            new("PS над уровнем ПДКВ [см вод.ст.]", ps),
            new("Pmax [см вод.ст.]", pmax),
            new("Pt [см вод.ст.]", pt),
            new("Триггерное окно [%]", triggerWindow),
        };

        var messages = new List<string>();

        if (expTrigger == 0xFF)
        {
            messages.Add("Триггер окончания вдоха: AUTO");
        }
        else
        {
            numericData.Add(new NumericDataItem("Триггер окончания вдоха [% от Finsp_max]", expTrigger));
        }

        return new PackageData(numericData.ToArray(), messages.ToArray());
    }
}