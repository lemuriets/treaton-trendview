namespace LogDecoder.CAN.Protocol.Definitions;

/// <summary>
/// Conditional numeric output: when <see cref="When"/> matches, emit a
/// NumericDataItem named <see cref="Name"/> from the field's raw value
/// (after <see cref="Scale"/>).
/// </summary>
public sealed class NumericEmit
{
    public WhenClause? When { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Scale { get; set; } = 1.0;
    public string? Unit { get; set; }
}
