using System.Text;
using System.Text.RegularExpressions;
using LogDecoder.CAN.Protocol.Definitions;
using LogDecoder.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LogDecoder.CAN.Protocol;

public sealed record LoadedProtocol(
    string FamilyName,
    ProtocolDefinition Protocol,
    IReadOnlyList<PackageDefinition> Packages);

/// <summary>One available machine family (a subfolder of the config root).</summary>
public sealed record ConfigFamily(string FolderName, string DisplayName, string FullPath);

/// <summary>
/// Loads the active machine family from a config folder laid out as
/// <c>config/&lt;family&gt;/{protocol.yaml, *.yaml}</c>.
/// </summary>
public static class ProtocolLoader
{
    public const string ConfigFolderName = "config";
    public const string ConfigDirEnvVar = "LOGDECODER_CONFIG_DIR";
    private const string ManifestFileName = "manifest.yaml";
    private const string DefaultPackagesGlob = "[0-9]*.yaml";

    /// <summary>
    /// Config root: the <c>LOGDECODER_CONFIG_DIR</c> env var if set, otherwise a
    /// <c>config</c> folder next to the executable.
    /// </summary>
    public static string DefaultConfigRoot
    {
        get
        {
            var overridden = Environment.GetEnvironmentVariable(ConfigDirEnvVar);
            return string.IsNullOrWhiteSpace(overridden)
                ? Path.Combine(AppPaths.BaseDirectory, ConfigFolderName)
                : overridden;
        }
    }

    /// <summary>Lists the available machine families (subfolders of the config root).</summary>
    public static IReadOnlyList<ConfigFamily> ListFamilies(string? configRoot = null, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        configRoot ??= DefaultConfigRoot;

        if (!Directory.Exists(configRoot))
        {
            logger.LogError("Config folder not found: {ConfigRoot}", configRoot);
            throw new ConfigFolderMissingException(configRoot);
        }

        var deserializer = BuildDeserializer();
        var families = new List<ConfigFamily>();
        foreach (var dir in Directory.GetDirectories(configRoot).OrderBy(d => d, StringComparer.Ordinal))
        {
            var folderName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar));
            if (!File.Exists(Path.Combine(dir, ManifestFileName)))
            {
                logger.LogWarning("Skipping config folder without {Manifest}: {Dir}", ManifestFileName, dir);
                continue;
            }
            var protocol = LoadManifest(deserializer, dir, logger).Protocol;
            var displayName = string.IsNullOrWhiteSpace(protocol.Name) ? folderName : protocol.Name!;
            families.Add(new ConfigFamily(folderName, displayName, dir));
        }

        logger.LogDebug("Found {Count} config families in {ConfigRoot}", families.Count, configRoot);
        return families;
    }

    public static LoadedProtocol LoadActiveFamily(string? configRoot = null, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        configRoot ??= DefaultConfigRoot;

        logger.LogDebug("Loading protocol config from {ConfigRoot}", configRoot);

        if (!Directory.Exists(configRoot))
        {
            logger.LogError("Config folder not found: {ConfigRoot}", configRoot);
            throw new ConfigFolderMissingException(configRoot);
        }

        var families = Directory.GetDirectories(configRoot);
        if (families.Length == 0)
        {
            logger.LogError("Config folder has no machine family subfolders: {ConfigRoot}", configRoot);
            throw new ConfigFolderMissingException(configRoot);
        }
        if (families.Length > 1)
        {
            var names = families.Select(Path.GetFileName).ToArray();
            logger.LogError("Multiple machine families found in {ConfigRoot}: {Families}", configRoot, string.Join(", ", names));
            throw new AmbiguousConfigException(configRoot, names!);
        }

        return LoadFamily(families[0], logger);
    }

    public static LoadedProtocol LoadFamily(string familyDir, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        logger.LogDebug("Loading machine family from {FamilyDir}", familyDir);

        var deserializer = BuildDeserializer();

        var manifest = LoadManifest(deserializer, familyDir, logger);
        var protocol = manifest.Protocol;
        var packages = LoadPackages(deserializer, familyDir, manifest.PackagesPath, logger);

        if (packages.Count == 0)
        {
            logger.LogError("No package definitions found in {FamilyDir}", familyDir);
            throw new ConfigValidationException($"No package definitions found in {familyDir}");
        }

        if (protocol.SynchroId <= 0 || packages.All(p => p.Id != protocol.SynchroId))
        {
            logger.LogError("Manifest synchroId {SynchroId} is not among loaded packages in {FamilyDir}",
                protocol.SynchroId, familyDir);
            throw new ConfigValidationException(
                $"Manifest synchroId {protocol.SynchroId} is not among loaded packages in {familyDir}");
        }

        var familyName = Path.GetFileName(familyDir.TrimEnd(Path.DirectorySeparatorChar));
        logger.LogInformation(
            "Loaded protocol family '{Family}' (revision {Revision}): {Count} packages",
            familyName, protocol.Revision ?? "n/a", packages.Count);

        return new LoadedProtocol(familyName, protocol, packages);
    }

    private static ProtocolManifest LoadManifest(IDeserializer deserializer, string familyDir, ILogger logger)
    {
        var manifestPath = Path.Combine(familyDir, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            logger.LogWarning("Manifest {Manifest} not found in {FamilyDir}; using defaults", ManifestFileName, familyDir);
            return new ProtocolManifest();
        }
        try
        {
            var manifest = deserializer.Deserialize<ProtocolManifest>(File.ReadAllText(manifestPath));
            return manifest ?? new ProtocolManifest();
        }
        catch (YamlException ex)
        {
            throw new ConfigValidationException($"Failed to parse {manifestPath}: {ex.Message}", ex);
        }
    }

    private static List<PackageDefinition> LoadPackages(IDeserializer deserializer, string familyDir, string? packagesPath, ILogger logger)
    {
        var packages = new List<PackageDefinition>();
        var seenIds = new HashSet<int>();

        var matches = BuildPackageMatcher(packagesPath);
        var files = Directory.EnumerateFiles(familyDir, "*")
            .Where(f => !Path.GetFileName(f).Equals(ManifestFileName, StringComparison.OrdinalIgnoreCase))
            .Where(f => matches(Path.GetFileName(f)))
            .OrderBy(f => f, StringComparer.Ordinal);

        foreach (var file in files)
        {
            PackageDefinition? def;
            try
            {
                def = deserializer.Deserialize<PackageDefinition>(File.ReadAllText(file));
            }
            catch (YamlException ex)
            {
                logger.LogError(ex, "Failed to parse package file {File}", file);
                throw new ConfigValidationException($"Failed to parse {file}: {ex.Message}", ex);
            }

            if (def is null)
            {
                logger.LogError("Empty package definition: {File}", file);
                throw new ConfigValidationException($"Empty package definition: {file}");
            }

            Validate(def, file, logger);

            if (!seenIds.Add(def.Id))
            {
                logger.LogError("Duplicate package id {Id} (file {File})", def.Id, Path.GetFileName(file));
                throw new ConfigValidationException($"Duplicate package id {def.Id} (file {Path.GetFileName(file)})");
            }

            logger.LogTrace("Loaded package {Id} '{Name}' ({FieldCount} fields)", def.Id, def.Name, def.Fields.Count);
            packages.Add(def);
        }

        return packages;
    }

    private static void Validate(PackageDefinition def, string file, ILogger logger)
    {
        var name = Path.GetFileName(file);
        if (def.Id <= 0)
        {
            logger.LogError("Package id missing or invalid in {File}", name);
            throw new ConfigValidationException($"Package id missing or invalid in {name}");
        }

        foreach (var field in def.Fields)
        {
            foreach (var b in EnumerateBytes(field))
            {
                if (b < 0 || (def.Length > 0 && b >= def.Length))
                {
                    logger.LogError("Field '{Field}' in {File} references byte {Byte} outside length {Length}",
                        field.Name, name, b, def.Length);
                    throw new ConfigValidationException(
                        $"Field '{field.Name}' in {name} references byte {b} outside length {def.Length}");
                }
            }

            if (field.Kind == FieldKind.Bit && field.Bit is { } bit && bit is < 0 or > 7)
            {
                logger.LogError("Field '{Field}' in {File} has bit {Bit} outside 0..7", field.Name, name, bit);
                throw new ConfigValidationException($"Field '{field.Name}' in {name} has bit {bit} outside 0..7");
            }

            if (field.Kind == FieldKind.BitRange && field.BitRange is { } range &&
                (range.From < 0 || range.To > 7 || range.From > range.To))
            {
                logger.LogError("Field '{Field}' in {File} has invalid bitRange {From}..{To}",
                    field.Name, name, range.From, range.To);
                throw new ConfigValidationException(
                    $"Field '{field.Name}' in {name} has invalid bitRange {range.From}..{range.To}");
            }
        }
    }

    private static IEnumerable<int> EnumerateBytes(FieldDefinition field)
    {
        if (field.Byte is { } single)
        {
            yield return single;
        }
        if (field.Bytes is { } bytes)
        {
            foreach (var b in bytes)
            {
                yield return b;
            }
        }
        if (field.Gate is { } gate)
        {
            yield return gate.Byte;
        }
    }

    /// <summary>
    /// Translates the manifest's <c>packagesPath</c> shell glob (supporting <c>*</c>,
    /// <c>?</c> and <c>[...]</c> character classes) into a filename predicate. This glob
    /// is the sole selector of package files; an absent pattern falls back to the
    /// <see cref="DefaultPackagesGlob"/> convention.
    /// </summary>
    private static Func<string, bool> BuildPackageMatcher(string? packagesPath)
    {
        var glob = string.IsNullOrWhiteSpace(packagesPath) ? DefaultPackagesGlob : packagesPath;

        var pattern = new StringBuilder("^");
        for (var i = 0; i < glob.Length; i++)
        {
            var c = glob[i];
            switch (c)
            {
                case '*':
                    pattern.Append(".*");
                    break;
                case '?':
                    pattern.Append('.');
                    break;
                case '[':
                    var close = glob.IndexOf(']', i + 1);
                    if (close < 0)
                    {
                        pattern.Append("\\[");
                        break;
                    }
                    pattern.Append(glob, i, close - i + 1);
                    i = close;
                    break;
                default:
                    pattern.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }
        pattern.Append('$');

        var regex = new Regex(pattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return name => regex.IsMatch(name);
    }

    private static IDeserializer BuildDeserializer()
    {
        return new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new ValueMessageConverter())
            .IgnoreUnmatchedProperties()
            .Build();
    }
}
