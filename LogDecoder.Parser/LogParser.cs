using System.Text.RegularExpressions;
using LogDecoder.CAN;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using LogDecoder.Parser.Data;
using LogDecoder.Parser.Contracts;
using Microsoft.Extensions.Logging;

namespace LogDecoder.Parser;

public partial class LogParser : ILogParser
{
    public LogParser(ILogger logger, string avlLogsFolder, ICanPackageFactory factory)
    {
        _logger = logger;
        _indexFolder = Path.Combine(avlLogsFolder, "index");
        _filesAggrerator = new LogFilesAggregator(avlLogsFolder, Path.GetFileName, FilenameTemplateRegex());
        _factory = factory;
        _indexBuilder = new IndexBuilder(logger, _factory);
        _indexParser = new IndexParser(logger);

        RegisteredIds = _factory.RegisteredIds;
        IdsWithNames = _factory.GetIdsWithNames();
        SortedBinFiles = _filesAggrerator.SortedFiles;
        
        Directory.CreateDirectory(_indexFolder);
    }

    public event Action? StartIndex;
    public event Action? FinishIndex;

    private readonly ILogger _logger;
    private readonly ICanPackageFactory _factory;
    private readonly string _indexFolder;
    private readonly LogFilesAggregator _filesAggrerator;
    private readonly IndexBuilder _indexBuilder;
    private readonly IndexParser _indexParser;
    private readonly Dictionary<string, LogFileScanner> _scanners = new();

    public readonly IReadOnlySet<int> RegisteredIds;
    public readonly List<(int Id, string Name)> IdsWithNames;
    public readonly IReadOnlyList<string> SortedBinFiles;
    
    public bool IsDateTimeExists(DateTime target) => _indexParser.IsDateTimeExists(target);
    public DateTime? GetStartDatetime() => _indexParser.FirstTime;
    public DateTime? GetLastDatetime() => _indexParser.LastTime;
    
    public void CreateOrLoadAllIndexes()
    {
        var indexFiles = new List<string>();
        StartIndex?.Invoke();
        foreach (var file in _filesAggrerator.SortedFiles)
        {
            indexFiles.Add(_indexBuilder.Build(file, _indexFolder));
        }
        _indexParser.LoadAll(indexFiles.ToArray());
        FinishIndex?.Invoke();
    }
    
    public async Task CreateOrLoadAllIndexesAsync(CancellationToken cancellationToken = default)
    {
        var indexFiles = new List<string>();

        StartIndex?.Invoke();

        await Task.Run(() =>
        {
            foreach (var file in _filesAggrerator.SortedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                indexFiles.Add(_indexBuilder.Build(file, _indexFolder));
            }
            _indexParser.LoadAll(indexFiles.ToArray());

        }, cancellationToken);

        FinishIndex?.Invoke();
    }
    
    public IEnumerable<ICanPackageParsed> GetPackages(IReadOnlySet<int> filterIds, DateTime start, DateTime end)
    {
        var startIndex = _indexParser.FindFloor(start);
        var endIndex = _indexParser.FindFloor(end);
        if (startIndex is null)
        {
            _logger.LogWarning("Unable to find {Start} in index.", start);
            throw new Exception($"Unable to find {start} in index.");
        }
        if (endIndex is null)
        {
            _logger.LogWarning("Unable to find {End} in index.", end);
            throw new Exception($"Unable to find {end} in index.");
        }
        
        var startFilename = startIndex.Value.Filename;
        var endFilename = endIndex.Value.Filename;

        var context = new ParseContext();
        
        _logger.LogInformation("LogParser.GetPackages(), start: ({StartFilename}: {StartIndexOffset}), end: ({EndFilename}: {EndIndexOffset})",
            startFilename,
            startIndex.Value.Offset,
            endFilename,
            endIndex.Value.Offset);
        foreach (var file in _filesAggrerator.GetRange(startFilename, endFilename))
        {
            var filename = Path.GetFileNameWithoutExtension(file);
            var scanner = GetScanner(file);
            
            var (startOffset, endOffset) = ResolveScanRange(filename, startFilename, endFilename, startIndex.Value.Offset, endIndex.Value.Offset);
            foreach (var (_, package) in scanner.GetPackagesParsed(_factory, filterIds, context, startOffset, endOffset))
            {
                yield return package;
            }
        }
    }
    
    private (long, long) ResolveScanRange(string filename, string startFilename, string endFilename, long startOffset, long endOffset)
    {
        if (startFilename == endFilename)
        {
            if (filename == startFilename)
            {
                return (startOffset, endOffset);
            }
            return (0, 0);
        }
        if (filename == startFilename)
        {
            return (startOffset, 0);
        }
        if (filename == endFilename)
        {
            return (0, endOffset);
        }
        return (0, 0);
    }

    
    private LogFileScanner GetScanner(string file)
    {
        if (_scanners.TryGetValue(file, out var scanner))
        {
            return scanner;
        }
        scanner = new LogFileScanner(_logger, file);
        _scanners[file] = scanner;
        return scanner;
    }

    [GeneratedRegex(@"^[0-1][0-9]$")]
    public static partial Regex FilenameTemplateRegex();
}