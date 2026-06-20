namespace LogDecoder.CAN.Protocol.Definitions;

public sealed class ValueMessageDefinition
{
    public string? Message { get; init; }

    public PackageTechStatus Status { get; init; } = PackageTechStatus.Ok;
}