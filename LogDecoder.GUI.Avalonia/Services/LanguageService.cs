using LogDecoder.GUI.Avalonia.Localization;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>Switches and persists the UI language.</summary>
public sealed class LanguageService(LanguageSettingsService settings)
{
    public string CurrentCulture => LocalizationManager.Instance.CurrentCultureName;

    public string LoadSavedLanguage() => settings.LoadLanguageOrDefault();

    public void SetLanguage(string culture)
    {
        LocalizationManager.Instance.SetCulture(culture);
        settings.SaveLanguage(culture);
    }
}
