using LogDecoder.GUI.Avalonia.Localization;
using LogDecoder.GUI.Avalonia.Services;

namespace LogDecoder.GUI.Avalonia.Models;

public class PackageItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public string Title => $"({Id}) {Name}";

    public string Description
    {
        get
        {
            var summary = PackageDescriptionService.Instance.GetSummary(Id);
            if (summary is not null)
            {
                return summary;
            }
            return PackageDescriptionService.Instance.HasEntry(Id)
                ? string.Empty
                : LocalizationManager.Instance.Get("PackageDescriptionMissing");
        }
    }

    public string Details => PackageDescriptionService.Instance.GetDetails(Id) ?? string.Empty;

    public bool HasDescription => Description.Length != 0;
    public bool HasDetails => Details.Length != 0;
}
