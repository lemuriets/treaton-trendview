using LogDecoder.Parser;
using LogDecoder.Parser.Export;

namespace LogDecoder.GUI.Avalonia.Services;

/// <summary>A parser plus its CSV exporter for a single selected logs folder.</summary>
public sealed record LogParsingSession(LogParser Parser, CsvExport Export);
