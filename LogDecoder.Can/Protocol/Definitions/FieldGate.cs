namespace LogDecoder.CAN.Protocol.Definitions;

/// <summary>
/// Conditional emit: the field is only produced when <c>Data[Byte] == Value</c>.
/// Mirrors C# packages that parse certain fields only in a given device state
/// (e.g. capno2 emits status/priority only when the "presence" byte is 0).
/// </summary>
public sealed class FieldGate
{
    public int Byte { get; set; }
    public long Value { get; set; }
}
