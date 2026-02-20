using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4A6, "Графическая информация")]
public class IdWaveCivl : BasePackageParsed
{
    public IdWaveCivl(CanPackage package, string name): base(package, name) { }
    
    public const int Id = 0x4A6;

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }
        // поток приведенный к тройнику [0.1 л/мин]
        var flow = BitUtil.ToS16(Data[0], Data[1]) * 0.1;
        // давление в магистрали [мм вод.ст.] -> см вод. ст
        var paw = BitUtil.ToS16(Data[2], Data[3]) / 10;
        // объем [0.1 мл]
        var vol = BitUtil.ToS32(Data[4], Data[5], Data[6], Data[7]) * 0.1;
        
        var numericData = new NumericDataItem[]
        {
            new("flow", flow),
            new("paw", paw),
            new("vol", vol)
        };;
        var messages = Array.Empty<string>();

        return new PackageData(numericData, messages);
    }
}