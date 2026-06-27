using LogDecoder.CAN.Packages;
using LogDecoder.CAN.Protocol;
using Microsoft.Extensions.Logging;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>
/// Builds a package factory from a chosen YAML protocol family (loaded from the
/// external config folder next to the executable).
/// </summary>
public static class ProtocolBootstrap
{
    public static CanPackageFactory CreateFactory(ConfigFamily family, ILogger logger)
    {
        var loaded = ProtocolLoader.LoadFamily(family.FullPath, logger);
        var factory = new CanPackageFactory();
        factory.LoadFrom(loaded, logger);
        return factory;
    }
}
