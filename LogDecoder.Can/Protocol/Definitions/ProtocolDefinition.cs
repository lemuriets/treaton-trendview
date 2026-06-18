namespace LogDecoder.CAN.Protocol.Definitions;

/// <summary>Root of a family folder's protocol.yaml.</summary>
public sealed class ProtocolManifest
{
    public ProtocolDefinition Protocol { get; set; } = new();

    /// <summary>Optional explicit file list; ignored when the loader globs the folder.</summary>
    public List<string>? PackageFiles { get; set; }
}

/// <summary>The protocol: node of a family manifest.</summary>
public sealed class ProtocolDefinition
{
    public string? Name { get; set; }
    public string? Revision { get; set; }
    public string? Document { get; set; }
    public int CanIdBits { get; set; } = 11;
    public Endianness Endianness { get; set; } = Endianness.Little;
}
