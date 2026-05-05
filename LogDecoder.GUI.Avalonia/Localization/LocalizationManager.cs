using System.ComponentModel;
using System.Globalization;
using System.Resources;
using LogDecoder.GUI.Avalonia.Lang;

namespace LogDecoder.GUI.Avalonia.Localization;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    private readonly ResourceManager _resourceManager = Resources.ResourceManager;

    public static LocalizationManager Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public string Get(string key)
    {
        return this[key];
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key), args);
    }

    public void SetCulture(string cultureName)
    {
        var culture = new CultureInfo(cultureName);

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        Resources.Culture = culture;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}