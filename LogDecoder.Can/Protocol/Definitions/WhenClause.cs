namespace LogDecoder.CAN.Protocol.Definitions;

/// <summary>
/// Equality-match guard for conditional fields. A null member is a wildcard.
/// <see cref="Mode"/> is matched against ParseContext.CivlMode; <see cref="Code"/>
/// against the field's own extracted raw value.
/// </summary>
public sealed class WhenClause
{
    public int? Mode { get; set; }
    public int? Code { get; set; }
}
