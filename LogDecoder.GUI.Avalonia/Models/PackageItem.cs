using LogDecoder.GUI.Avalonia.Localization;

namespace LogDecoder.GUI.Avalonia.Models;

public class PackageItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public string Title => $"({Id}) {Name}";

    public string Description => LocalizationManager.Instance.Find($"PkgDesc_{Id}") ?? string.Empty;
    public string Details => LocalizationManager.Instance.Find($"PkgInfo_{Id}") ?? string.Empty;

    public bool HasDescription => Description.Length != 0;
    public bool HasDetails => Details.Length != 0;
}
