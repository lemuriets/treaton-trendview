using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x5A9, "Состояние КМ")]
public class IdStatusMotor : BasePackageParsed
{
    public const int Id = 0x5A9;
    
    private static readonly Dictionary<int,(string,PackageTechStatus)> BitsDefinitions = new()
    {
        { 0, ("Отказ датчиков Холла", PackageTechStatus.Error) },
        { 1, ("Отказ драйвера ЭД (OCTW)", PackageTechStatus.Error) },
        { 2, ("Отказ драйвера ЭД (FAULT)", PackageTechStatus.Error) },
        { 3, ("Флаг сброса", PackageTechStatus.Info) },
        { 5, ("Отказ энкодера", PackageTechStatus.Error) },
        { 6, ("Отказ привода", PackageTechStatus.Error) },
        { 7, ("КРИТИЧЕСКИЙ ОТКАЗ (останов мотора)", PackageTechStatus.Critical) }
    };

    public IdStatusMotor(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 1)
        {
            return null;
        }
        var numericData = Array.Empty<NumericDataItem>();
        var messages = ParseBits(Data[0], BitsDefinitions);

        return new PackageData(numericData, messages);
    }
}
