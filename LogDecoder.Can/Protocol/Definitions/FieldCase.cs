namespace LogDecoder.CAN.Protocol.Definitions;

/// <summary>
/// A conditional message table: when <see cref="When"/> matches, the field's
/// extracted value is looked up in <see cref="Values"/> to produce a message.
/// </summary>
public sealed class FieldCase
{
    public WhenClause? When { get; set; }
    public Dictionary<int, ValueMessageDefinition> Values { get; set; } = new();
}
