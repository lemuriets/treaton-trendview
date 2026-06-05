using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using LogDecoder.GUI.Avalonia.Models;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>Exposes the selectable CAN packages and the IDs that are always exported.</summary>
public sealed class PackageCatalog(ICanPackageFactory factory)
{
    public IReadOnlySet<int> RequiredExportIds { get; } = new HashSet<int> { IdSynchro.Id };

    public IReadOnlyList<PackageItem> GetExportablePackages()
    {
        return factory
            .GetIdsWithNames(excludeIds: RequiredExportIds)
            .Select(p => new PackageItem { Id = p.Id, Name = p.Name })
            .OrderBy(p => p.Id)
            .ToList();
    }
}
