using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

public class BasePackageParsed(CanPackage basePackage, string name) : ICanPackageParsed
{
    public int Id { get; } = basePackage.Id;
    public PackageType Type { get; } = basePackage.Type;
    public ReadOnlyMemory<byte> Data { get; } = basePackage.Data;
    public int Hrc { get; set; } = basePackage.Hrc;
    public int Length { get; } = basePackage.Length;
    public string Name {  get; } = name;
    public PackageTechStatus TechStatus { get; protected set; } = PackageTechStatus.Ok;

    public virtual PackageData? ParseData()
    {
        throw new NotImplementedException();
    }

    public override string ToString() => string.Join(' ', Data);
}


