using System.Globalization;

namespace LogDecoder.Parser.Data;

public readonly record struct IndexFile(
    IndexFileHeader Header,
    IReadOnlyList<IndexEntry> Entries);

// mb should make init validation and ToString for IndexBuilder
public readonly record struct IndexEntry(
    string Filename,
    long Offset,
    DateTime Time);

public readonly record struct IndexFileHeader(
    int FormatVersion,
    string SourceFile)
{
    public IndexFileHeader(string? header)
        : this(Parse(header))
    {
    }

    private IndexFileHeader(Dictionary<string, string> values)
        : this(
            int.Parse(values[nameof(FormatVersion)], CultureInfo.InvariantCulture),
            values[nameof(SourceFile)])
    {
    }

    public override string ToString()
    {
        return string.Join("; ", $"FormatVersion {FormatVersion}", $"SourceFile {SourceFile}");
    }

    private static Dictionary<string, string> Parse(string? header)
    {
        if (header is null)
        {
            return new Dictionary<string, string>();
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(header);

        var result = new Dictionary<string, string>();

        var parts = header.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var pair = part.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (pair.Length != 2)
            {
                throw new FormatException($"Invalid header part: {part}");
            }

            result[pair[0]] = pair[1];
        }

        if (!result.ContainsKey(nameof(FormatVersion)))
        {
            throw new FormatException($"Header does not contain {nameof(FormatVersion)}.");
        }
        if (!result.ContainsKey(nameof(SourceFile)))
        {
            throw new FormatException($"Header does not contain {nameof(SourceFile)}.");
        }

        return result;
    }
}