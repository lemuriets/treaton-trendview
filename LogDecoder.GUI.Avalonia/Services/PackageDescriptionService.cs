using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LogDecoder.GUI.Avalonia.Services;

public sealed class PackageDescriptionService
{
    private const string FolderName = "descriptions";
    private const string FallbackLanguage = "ru";

    public sealed class LocalizedText
    {
        public string? Summary { get; set; }
        public string? Details { get; set; }
    }

    private sealed class DescriptionFile
    {
        public int PackageId { get; set; }
        public Dictionary<string, LocalizedText> Descriptions { get; set; } = new();
    }

    public static PackageDescriptionService Instance { get; private set; } = new();

    private readonly Dictionary<int, Dictionary<string, LocalizedText>> _byId = new();

    private PackageDescriptionService()
    {
    }

    public static void Initialize(string familyDir, ILogger? logger = null)
    {
        Instance = Load(familyDir, logger ?? NullLogger.Instance);
    }

    public bool HasEntry(int id) => _byId.ContainsKey(id);

    public string? GetSummary(int id) => Resolve(id)?.Summary;

    public string? GetDetails(int id) => Resolve(id)?.Details;

    private LocalizedText? Resolve(int id)
    {
        if (!_byId.TryGetValue(id, out var byLang))
        {
            return null;
        }

        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (byLang.TryGetValue(lang, out var text))
        {
            return text;
        }
        if (byLang.TryGetValue(FallbackLanguage, out var fallback))
        {
            return fallback;
        }

        foreach (var any in byLang.Values)
        {
            return any;
        }
        return null;
    }

    private static PackageDescriptionService Load(string familyDir, ILogger logger)
    {
        var service = new PackageDescriptionService();
        var dir = Path.Combine(familyDir, FolderName);
        if (!Directory.Exists(dir))
        {
            logger.LogInformation("No package descriptions folder at {Dir}", dir);
            return service;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        foreach (var file in Directory.EnumerateFiles(dir, "*.yaml"))
        {
            DescriptionFile? parsed;
            try
            {
                parsed = deserializer.Deserialize<DescriptionFile>(File.ReadAllText(file));
            }
            catch (YamlException ex)
            {
                logger.LogWarning(ex, "Failed to parse description file {File}; skipping", file);
                continue;
            }

            if (parsed is null || parsed.PackageId <= 0)
            {
                logger.LogWarning("Description file {File} has no valid packageId; skipping", file);
                continue;
            }

            var name = Path.GetFileNameWithoutExtension(file);
            if (int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fileId)
                && fileId != parsed.PackageId)
            {
                logger.LogWarning(
                    "Description file {File} name does not match packageId {PackageId}; using packageId",
                    Path.GetFileName(file), parsed.PackageId);
            }

            if (!service._byId.TryAdd(parsed.PackageId, parsed.Descriptions))
            {
                logger.LogWarning(
                    "Duplicate packageId {PackageId} (file {File}); keeping the first",
                    parsed.PackageId, Path.GetFileName(file));
            }
        }

        logger.LogInformation("Loaded descriptions for {Count} packages from {Dir}", service._byId.Count, dir);
        return service;
    }
}
