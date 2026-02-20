using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

public class UnknownCanPackage : ICanPackageParsed
{
    public static readonly UnknownCanPackage Instance = new();

    private UnknownCanPackage() { }
    
    public int Id => 0;
    public PackageType Type => PackageType.Empty;
    public byte[] Data => [];
    public int Hrc { get; set; }
    public int Length => 0;
    public string Name => "";
    public PackageTechStatus TechStatus => PackageTechStatus.Ok;

    public PackageData? ParseData()
    {
        return null;
    }
}