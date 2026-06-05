using LogDecoder.GUI.Avalonia.Localization;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>Switches and persists the UI language.</summary>
public sealed class LanguageService(LanguageSettingsService settings)
{
    public string CurrentCulture => LocalizationManager.Instance.CurrentCultureName;

    /// <summary>Applies the saved language (or the default) at startup, without re-saving it.</summary>
    public void ApplySavedLanguage()
    {
        LocalizationManager.Instance.SetCulture(settings.LoadLanguageOrDefault());
    }

    public void SetLanguage(string culture)
    {
        LocalizationManager.Instance.SetCulture(culture);
        settings.SaveLanguage(culture);
    }
}
