namespace LogDecoder.CAN.Protocol.Definitions;

public sealed class BitRangeDefinition
{
    public required int From { get; init; }
    public required int To { get; init; }
}