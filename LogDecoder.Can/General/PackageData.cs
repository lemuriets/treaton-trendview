using System.Runtime.CompilerServices;

namespace LogDecoder.CAN.General;

public readonly record struct NumericDataItem(string Name, double Value)
{
    public override string ToString()
    {
        return $"{Name}: {Value}";
    }
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct PackageData(IReadOnlyCollection<NumericDataItem> numericData, List<string> messages)
{
    public readonly IReadOnlyCollection<NumericDataItem> NumericData = numericData;
    public readonly List<string> Messages = messages;
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct EmptyPackageData()
{
    
}
