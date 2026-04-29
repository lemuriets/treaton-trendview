using LogDecoder.CAN.CanPackages;
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
    
    protected List<string> ParseBitsAndUpdateStatus<T>(
        T value,
        Dictionary<int, BitMessage> bitDefinitions)
        where T : struct, IConvertible
    {
        var results = new List<string>();
        var bits = Convert.ToUInt64(value);

        foreach (var (bit, bitMessage) in bitDefinitions)
        {
            if (bit is < 0 or >= 64)
            {
                continue;
            }

            var isSet = BitUtil.IsBitSet(bits, bit);
            if (!bitMessage.TryGetMessage(isSet, out var message))
            {
                continue;
            }
            results.Add(message);
            UpdateTechStatus(bitMessage.Status);
        }
        return results;
    }
    
    protected List<string> ParseValueMessageAndUpdateStatus(
        ulong value,
        IReadOnlyDictionary<ulong, MessageDefinition> definitions)
    {
        if (!definitions.TryGetValue(value, out var definition))
        {
            return [];
        }
        UpdateTechStatus(definition.Status);

        return [definition.Message];

    }
    
    private void UpdateTechStatus(PackageTechStatus status)
    {
        TechStatus = (PackageTechStatus)Math.Max((int)TechStatus, (int)status);
    }
}


