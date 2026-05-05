using LogDecoder.GUI.Avalonia.Localization;

namespace LogDecoder.GUI.Avalonia;

public static class Localizer
{
    public static string L(string key)
    {
        return LocalizationManager.Instance.Get(key);
    }

    public static string F(string key, params object[] args)
    {
        return LocalizationManager.Instance.Format(key, args);
    }
}