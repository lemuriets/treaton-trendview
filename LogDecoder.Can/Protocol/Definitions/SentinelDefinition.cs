namespace LogDecoder.CAN.Protocol.Definitions;

/// <summary>
/// A "magic value" on a numeric field: when the field's raw value equals
/// <see cref="Value"/>, emit <see cref="Message"/> instead of the numeric item
/// (e.g. 0xFFFF meaning "auto").
/// </summary>
public sealed class SentinelDefinition
{
    public long Value { get; set; }
    public string? Message { get; set; }

    /// <summary>Default Ok: C# emits these "auto" messages without escalating status.</summary>
    public PackageTechStatus Status { get; set; } = PackageTechStatus.Ok;
}
