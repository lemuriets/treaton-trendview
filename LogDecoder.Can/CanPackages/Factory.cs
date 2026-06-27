using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Protocol;
using LogDecoder.CAN.Protocol.Definitions;
using LogDecoder.Parser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LogDecoder.CAN.Packages;

internal record FactoryItem(string PackageName, Func<CanPackage, string, ParseContext, ICanPackageParsed> Constructor);

public class CanPackageFactory : ICanPackageFactory
{
    private readonly Dictionary<int, FactoryItem> _registered = new();
    private readonly Dictionary<int, int> _contextSetters = new();
    public IReadOnlySet<int> RegisteredIds => _registered.Keys.ToHashSet();
    public int SynchroId { get; private set; }

    public CanPackageFactory()
    {
    }

    /// <summary>Registers every package from a loaded YAML protocol family.</summary>
    public void LoadFrom(LoadedProtocol protocol, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        _registered.Clear();
        _contextSetters.Clear();
        SynchroId = protocol.Protocol.SynchroId;

        var endianness = protocol.Protocol.Endianness;
        foreach (var definition in protocol.Packages)
        {
            var def = definition;
            Register(def.Id, def.Name, (p, n, c) => new ConfigCanPackage(p, n, def, endianness, c));
            if (def.SetsContext is { } setter)
            {
                _contextSetters[def.Id] = setter.Byte;
            }
        }

        logger.LogDebug(
            "Registered {Count} packages from family '{Family}' ({Setters} context setters)",
            _registered.Count, protocol.FamilyName, _contextSetters.Count);
    }

    public List<(int Id, string Name)> GetIdsWithNames(IReadOnlySet<int>? excludeIds = null)
    {
        return _registered
            .Where(kvp => excludeIds == null || !excludeIds.Contains(kvp.Key))
            .Select(kvp => (kvp.Key, kvp.Value.PackageName))
            .ToList();
    }

    public ICanPackageParsed Create(CanPackage package, ParseContext context)
    {
        if (_contextSetters.TryGetValue(package.Id, out var byteIndex) && package.Data.Length > byteIndex)
        {
            context.CivlMode = package.Data.Span[byteIndex];
        }
        if (_registered.TryGetValue(package.Id, out var item))
        {
            return item.Constructor(package, item.PackageName, context);
        }
        return UnknownCanPackage.Instance;
    }

    public void Register(int id, string name, Func<CanPackage, string, ParseContext, ICanPackageParsed> constructor)
    {
        if (_registered.ContainsKey(id))
        {
            throw new InvalidOperationException($"Duplicate CANPackage Id: {id}");
        }

        _registered[id] = new FactoryItem(name, constructor);
    }
}
