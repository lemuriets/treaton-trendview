using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace LogDecoder.GUI.Avalonia.Localization;

public sealed class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension()
    {
    }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding($"[{Key}]")
        {
            Mode = BindingMode.OneWay,
            Source = LocalizationManager.Instance
        };
    }
}
