namespace LogDecoder.CAN.Protocol.Definitions;

public sealed class PackageDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Minimum data length; shorter payloads yield a null parse result.</summary>
    public int Length { get; set; }

    public string? Description { get; set; }

    /// <summary>If set, this package writes a byte into the shared ParseContext.</summary>
    public ContextSetter? SetsContext { get; set; }

    /// <summary>Name of the ParseContext value this package's conditionals read.</summary>
    public string? RequiresContext { get; set; }

    /// <summary>
    /// Constant messages always emitted when the package is present (e.g. zero-data
    /// event packages). Emitted before field-driven messages.
    /// </summary>
    public List<ValueMessageDefinition>? Messages { get; set; }

    public List<FieldDefinition> Fields { get; set; } = new();
}
