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

    public string CurrentCultureName => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

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
        // Newly created threads/Tasks (export, indexing) inherit these defaults, so
        // number/date formatting and resource lookups stay consistent off the UI thread.
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Resources.Culture = culture;

        // Avalonia 11 only re-reads an indexer binding ({Binding [Key]} / {loc:Localize})
        // when PropertyChanged fires with the CLR indexer member name "Item" — verified
        // empirically. Neither "" ("all changed") nor the WPF-style "Item[]" refresh it.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
    }
}