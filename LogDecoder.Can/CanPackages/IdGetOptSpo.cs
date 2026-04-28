using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4E5, "Информация о параметрах работы модуля")]
public class IdGetOptSpo : BasePackageParsed
{
    public const int Id = 0x4E5;

    public IdGetOptSpo(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }

        var span = Data.Span;

        var sampleRate = BitUtil.ToU16(span[0], span[1]);
        var currentAnalysisSettings = span[2];

        var numericData = new NumericDataItem[]
        {
            new("sample_rate", sampleRate),
            new("current_analysis_settings", currentAnalysisSettings),
        };
        var messages = new List<string>();

        return new PackageData(numericData, messages);
    }
}
