using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x581, "Состояние модуля пульсометрии Masimo 1")]
public class IdStatusSpoV21 : BasePackageParsed
{
    public IdStatusSpoV21(CanPackage p, string name) : base(p, name) { }
    
    public const int Id = 0x581;
    
    // TODO: добавить кодов и доработать парсинг
    private static readonly Dictionary<int, (string, PackageTechStatus)> BitsDefinitions = new()
    {
        { 0, ("quality: filling (PI) ok", PackageTechStatus.Ok) },
        { 1, ("quality: pulse (PR) ok", PackageTechStatus.Ok) },
        { 2, ("quality: spo2 ok", PackageTechStatus.Ok) },
        { 3, ("quality: pvi ok", PackageTechStatus.Ok) },
        { 4, ("quality: sensor type ok", PackageTechStatus.Ok) },
    };

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }
        
        var filling = BitUtil.ToU16(Data[0], Data[1]) * 0.001;
        var pulse = BitUtil.ToU16(Data[2], Data[3]);
        var spo2 = Data[4];
        var pvi = Data[5];
        var sensorStatus = Data[6];
        var qualityByte = Data[7];

        var sensorState = sensorStatus & 0x0F;
        var sensorType = (sensorStatus >> 4) & 0x0F;
        var mode = (qualityByte >> 6) & 0x03;

        var numericData = new NumericDataItem[]
        {
            new("filling", filling),
            new("pulse", pulse),
            new("spo2", spo2),
            new("pvi", pvi),
            new("sensor_state", sensorState),
            new("sensor_type", sensorType),
            new("mode", mode)
        };
        var messages = new List<string>();
        if (sensorState != 0)
        {
            messages.Add($"Sensor state code {sensorState}");
        }
        messages.AddRange(ParseBits(qualityByte, BitsDefinitions));

        return new PackageData(numericData, messages.ToArray());
    }
}