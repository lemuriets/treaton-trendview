using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

public class BasePackageParsed(CanPackage basePackage, string name) : ICanPackageParsed
{
    public int Id { get; } = basePackage.Id;
    public PackageType Type { get; } = basePackage.Type;
    public byte[] Data { get; } = basePackage.Data;
    public int Hrc { get; set; } = basePackage.Hrc;
    public int Length { get; } = basePackage.Length;
    public string Name {  get; } = name;
    public PackageTechStatus TechStatus { get; protected set; } = PackageTechStatus.Ok;

    public virtual PackageData? ParseData()
    {
        throw new NotImplementedException();
    }

    public override string ToString() => string.Join(' ', Data);
    
    protected string[] ParseBits<T>(
        T value,
        Dictionary<int, (string msg, PackageTechStatus status)> bitDefinitions)
        where T : struct, IConvertible
    {
        var results = new List<string>();

        foreach (var kv in bitDefinitions)
        {
            if (BitUtil.IsBitSet(Convert.ToUInt64(value), kv.Key))
            {
                results.Add(kv.Value.msg);
                TechStatus = (PackageTechStatus)Math.Max((int)TechStatus, (int)kv.Value.status);
            }
        }
        return results.ToArray();
    }
}
