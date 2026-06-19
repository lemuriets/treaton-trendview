namespace LogDecoder.CAN.Protocol.Definitions;

/// <summary>Root of a family folder's protocol.yaml.</summary>
public sealed class ProtocolManifest
{
    public ProtocolDefinition Protocol { get; set; } = new();

    /// <summary>Shell-style glob selecting the package files to load (e.g. <c>[0-9]*.yaml</c>).</summary>
    public string? PackagesPath { get; set; }
}

/// <summary>The protocol: node of a family manifest.</summary>
public sealed class ProtocolDefinition
{
    public string? Name { get; set; }
    public string? Revision { get; set; }
    public Endianness Endianness { get; set; } = Endianness.Little;

    /// <summary>Id of the synchronization package whose timestamp drives log indexing.</summary>
    public int SynchroId { get; set; }
}
