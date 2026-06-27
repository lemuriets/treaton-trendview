namespace LogDecoder.CAN.Protocol;

public class ProtocolLoadException : Exception
{
    public ProtocolLoadException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>The config folder (or its single family subfolder) is absent/empty.</summary>
public sealed class ConfigFolderMissingException : ProtocolLoadException
{
    public ConfigFolderMissingException(string configPath)
        : base($"Config folder not found or empty: {configPath}")
    {
        ConfigPath = configPath;
    }

    public string ConfigPath { get; }
}

/// <summary>More than one machine family is present and none is selected.</summary>
public sealed class AmbiguousConfigException : ProtocolLoadException
{
    public AmbiguousConfigException(string configPath, IEnumerable<string> families)
        : base($"Multiple machine families found in {configPath}: {string.Join(", ", families)}")
    {
        ConfigPath = configPath;
    }

    public string ConfigPath { get; }
}

/// <summary>A package/manifest file is malformed or fails validation.</summary>
public sealed class ConfigValidationException : ProtocolLoadException
{
    public ConfigValidationException(string message, Exception? inner = null) : base(message, inner) { }
}
