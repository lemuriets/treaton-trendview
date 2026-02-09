using System.Reflection;
using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.Contracts;

namespace LogDecoder.CAN.Packages;

internal class FactoryItem
{
    public FactoryItem(string packageName, Func<CanPackage, string, ICanPackageParsed> constructor)
    {
        PackageName = packageName;
        Constructor = constructor;
    }
    
    public Func<CanPackage, string, ICanPackageParsed> Constructor { get; }
    public string PackageName { get; }
}

public static class CanPackageFactory
{
    private static readonly Dictionary<int, FactoryItem> _registered = new();
    public static HashSet<int> RegisteredIds => _registered.Keys.ToHashSet();

    public static List<(int Id, string PackageName)> GetIdsWithNames()
    {
        return _registered
            .Select(kvp => (kvp.Key, kvp.Value.PackageName))
            .ToList();
    }

    static CanPackageFactory()
    {
        RegisterAllFromAssembly(Assembly.GetExecutingAssembly());
    }

    private static void RegisterAllFromAssembly(Assembly assembly)
    {
        var packageTypes = assembly.GetTypes()
            .Where(t => typeof(ICanPackageParsed).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => new
            {
                Type = t,
                Attr = t.GetCustomAttribute<CanPackageAttr>(),
            })
            .Where(x => x.Attr != null);

        foreach (var entry in packageTypes)
        {
            var pkgId = entry.Attr!.Id;
            var pkgName = entry.Attr!.Name;

            if (_registered.ContainsKey(pkgId))
                throw new InvalidOperationException($"Duplicate CANPackage Id: {pkgId}");
            
            _registered[pkgId] = new FactoryItem(
                pkgName,
                (package, name) => (ICanPackageParsed)Activator.CreateInstance(entry.Type, package, name)!
            );
        }
    }

    public static ICanPackageParsed Create(CanPackage package)
    {
        return !_registered.TryGetValue(package.Id, out var item)
            ? EmptyPackage.Instance
            : item.Constructor(package, item.PackageName);
    }
}
