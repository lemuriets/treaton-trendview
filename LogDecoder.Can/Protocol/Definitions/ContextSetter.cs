namespace LogDecoder.CAN.Protocol.Definitions;

/// <summary>
/// Marks a package as the source of a ParseContext value. The single byte at
/// <see cref="Byte"/> is captured into the context under <see cref="Key"/>
/// (currently only "civlMode" is consumed downstream).
/// </summary>
public sealed class ContextSetter
{
    public string Key { get; set; } = string.Empty;
    public int Byte { get; set; }
}
