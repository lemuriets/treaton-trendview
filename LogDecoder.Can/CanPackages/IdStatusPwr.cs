using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x401, "Состояние ИП")]
public class IdStatusPwr : BasePackageParsed
{
    public const int Id = 0x401;

    private static readonly Dictionary<int, (string msg, PackageTechStatus status)> BitsDefinitions = new()
    {
        { 1, ("Неисправен вентилятор", PackageTechStatus.Error) },
        { 2, ("Осталось менее 10 минут работы от аккумулятора", PackageTechStatus.Warning) },
        { 3, ("Авария АКБ: АКБ отсутствует", PackageTechStatus.Warning) },
        { 4, ("Авария АКБ: ток зарядки/КЗ", PackageTechStatus.Warning) },
        { 5, ("Авария АКБ: превышено напряжение зарядки", PackageTechStatus.Warning) },
        { 7, ("Неисправен динамик (обрыв)", PackageTechStatus.Warning) }
    };
    
    public IdStatusPwr(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 5)
        {
            return null;
        }
        var battery = Data[1]; 
        var statusBits = BitUtil.ToU16(Data[2], Data[3]);
        
        var numericData = new NumericDataItem[]
        {
            new("battery", battery)
        };
        var messages = ParseBits(statusBits, BitsDefinitions);

        return new PackageData(numericData, messages);
    }
}

