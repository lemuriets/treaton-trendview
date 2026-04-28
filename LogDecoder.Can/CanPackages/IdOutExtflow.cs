using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x541, "Информация о потоке")]
public class IdOutExtflow : BasePackageParsed
{
    public const int Id = 0x541;

    public IdOutExtflow(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 3)
        {
            return null;
        }

        var span = Data.Span;

        var flowRaw = BitUtil.ToU16(span[0], span[1]);

        var statusByte = span[2];
        var noDataFlag = (statusByte >> 7) & 0x01;
        var avgCount = statusByte & 0x7F;

        var numericData = new NumericDataItem[]
        {
            new("FlowRaw", flowRaw),
            new("AvgCount", avgCount)
        };

        var messages = new List<string>();

        if (noDataFlag == 1)
        {
            messages.Add("Нет данных от датчика потока (ошибка или инициализация)");
        }
        if (avgCount == 0)
        {
            messages.Add("Данные не обновлены (используется предыдущая точка)");
        }

        return new PackageData(numericData, messages);
    }
}
