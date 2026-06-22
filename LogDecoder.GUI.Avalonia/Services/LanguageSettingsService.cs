using System.Text.Json;
using LogDecoder.Helpers;
using Microsoft.Extensions.Logging;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>
/// GUI-owned persistence for the selected UI language. Stored in a small JSON file
/// next to the executable, intentionally separate from the Infrastructure userconfig.json.
/// </summary>
public sealed class LanguageSettingsService
{
    private const string FileName = "ui-settings.json";
    private const string DefaultLanguage = "ru";

    private readonly ILogger? _logger;

    public LanguageSettingsService(ILogger? logger = null)
    {
        _logger = logger;
    }

    private sealed class UiSettings
    {
        public string Language { get; set; } = DefaultLanguage;
    }

    public string LoadLanguageOrDefault()
    {
        var path = GetPath();
        try
        {
            if (!File.Exists(path))
            {
                return DefaultLanguage;
            }

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<UiSettings>(json);
            var language = settings?.Language;

            return string.IsNullOrWhiteSpace(language) ? DefaultLanguage : language;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read UI settings, falling back to {Default}", DefaultLanguage);
            return DefaultLanguage;
        }
    }

    public void SaveLanguage(string language)
    {
        var path = GetPath();
        try
        {
            var json = JsonSerializer.Serialize(
                new UiSettings { Language = language },
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to persist UI language {Language}", language);
        }
    }

    private static string GetPath()
    {
        return Path.Combine(AppPaths.BaseDirectory, FileName);
    }
}
