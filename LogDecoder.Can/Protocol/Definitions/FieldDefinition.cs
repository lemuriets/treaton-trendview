namespace LogDecoder.CAN.Protocol.Definitions;

public sealed class FieldDefinition
{
    public string Name { get; set; } = string.Empty;
    public FieldKind Kind { get; set; } = FieldKind.Value;

    /// <summary>Single source byte (mutually exclusive with <see cref="Bytes"/>).</summary>
    public int? Byte { get; set; }

    /// <summary>Multi-byte source, LSB-first unless <see cref="Endianness"/> overrides.</summary>
    public List<int>? Bytes { get; set; }

    /// <summary>Bit index within <see cref="Byte"/> for <see cref="FieldKind.Bit"/>.</summary>
    public int? Bit { get; set; }

    /// <summary>Bit range within <see cref="Byte"/> for <see cref="FieldKind.BitRange"/>.</summary>
    public BitRangeDefinition? BitRange { get; set; }

    public bool Signed { get; set; }
    public double Scale { get; set; } = 1.0;
    public string? Unit { get; set; }
    public Endianness? Endianness { get; set; }

    /// <summary>Added to the numeric value after scaling: raw * scale + offset.</summary>
    public double Offset { get; set; }

    /// <summary>Emit this field only when Data[gate.Byte] == gate.Value.</summary>
    public FieldGate? Gate { get; set; }

    /// <summary>Clamp the raw value to at most this (mirrors C# Math.Min(value, max)).</summary>
    public long? Max { get; set; }

    /// <summary>Clamp the raw value to at least this.</summary>
    public long? Min { get; set; }

    /// <summary>Magic-value handling: emit a message instead of the numeric item.</summary>
    public SentinelDefinition? Sentinel { get; set; }

    /// <summary>Year offset for <see cref="FieldKind.DateTime"/> (e.g. 2000).</summary>
    public int YearOffset { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Value-to-message table. When present the field emits a message (looked up by
    /// its extracted value); when absent the field emits a numeric data item.
    /// </summary>
    public Dictionary<int, ValueMessageDefinition>? Values { get; set; }

    /// <summary>Conditional message tables, selected by <see cref="WhenClause"/>.</summary>
    public List<FieldCase>? Cases { get; set; }

    /// <summary>Conditional numeric outputs, selected by <see cref="WhenClause"/>.</summary>
    public List<NumericEmit>? EmitNumeric { get; set; }
}
