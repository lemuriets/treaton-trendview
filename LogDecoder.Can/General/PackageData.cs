using System.Runtime.CompilerServices;

namespace LogDecoder.CAN.General;

public readonly record struct NumericDataItem(string Name, double Value);

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct PackageData(NumericDataItem[] numericData, string[] messages)
{
    public readonly NumericDataItem[] NumericData = numericData;
    public readonly string[] Messages = messages;
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct EmptyPackageData()
{
    
}
