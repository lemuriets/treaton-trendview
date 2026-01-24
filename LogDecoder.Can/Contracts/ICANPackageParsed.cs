using LogDecoder.CAN.General;
using LogDecoder.CAN.Packages;

namespace LogDecoder.CAN.Contracts;

public interface ICanPackageParsed
{
    int Id { get; }
    PackageType Type { get; }
    byte[] Data { get; }
    int Hrc { get; set; }
    int Length { get; }
    string Name { get; }
    PackageTechStatus TechStatus { get; }
    
    PackageData? ParseData();
}
