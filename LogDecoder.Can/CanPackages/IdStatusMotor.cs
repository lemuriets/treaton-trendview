using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x5A9, "Состояние контроллера мотора")]
public class IdStatusMotor : BasePackageParsed
{
    public const int Id = 0x5A9;
    
    private static readonly Dictionary<int, BitMessage> BitsDefinitions = new()
    {
        { 0, BitMessage.One("Отказ датчиков Холла", PackageTechStatus.Error) },
        { 1, BitMessage.One("Отказ драйвера ЭД (OCTW)", PackageTechStatus.Error) },
        { 2, BitMessage.One("Отказ драйвера ЭД (FAULT)", PackageTechStatus.Error) },
        { 3, BitMessage.One("Флаг сброса", PackageTechStatus.Info) },
        { 5, BitMessage.One("Отказ энкодера", PackageTechStatus.Error) },
        { 6, BitMessage.One("Отказ привода", PackageTechStatus.Error) },
        { 7, BitMessage.One("КРИТИЧЕСКИЙ ОТКАЗ (остановка мотора)", PackageTechStatus.Critical) }
    };

    public IdStatusMotor(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 1)
        {
            return null;
        }
        var span = Data.Span;

        var numericData = Array.Empty<NumericDataItem>();
        var messages = ParseBitsAndUpdateStatus(span[0], BitsDefinitions);

        return new PackageData(numericData, messages);
    }
}
