using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x5AB, "Скорость вращения мотора")]
public class IdRealSpeedMotor : BasePackageParsed
{
    public const int Id = 0x5AB;

    public IdRealSpeedMotor(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 3)
        {
            return null;
        }

        var span = Data.Span;

        var speedRaw = BitUtil.ToU16(span[0], span[1]) * 0.1; // 0.1 об/мин
        var status_value = span[2];

        var numericData = new NumericDataItem[]
        {
            new("speed_rpm_x10", speedRaw),
            new("status_value", status_value),
        };
        var messages = Array.Empty<string>();

        return new PackageData(numericData, messages);
    }
}
