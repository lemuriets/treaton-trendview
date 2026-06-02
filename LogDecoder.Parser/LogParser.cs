using System.Text.RegularExpressions;
using LogDecoder.CAN.Contracts;
using LogDecoder.Parser.Data;
using LogDecoder.Parser.Contracts;
using Microsoft.Extensions.Logging;

namespace LogDecoder.Parser;

public partial class LogParser : ILogParser
{
    public LogParser(ILogger logger, string avlLogsFolder, ICanPackageFactory factory)
    {
        _logger = logger;
        _avlLogsFolder = avlLogsFolder;
        _indexFolder = Path.Combine(avlLogsFolder, "index");
        _filesAggregator = new LogFilesAggregator(avlLogsFolder, Path.GetFileName, FilenameTemplateRegex());
        _factory = factory;
        _indexBuilder = new IndexBuilder(logger, _factory);
        _indexParser = new IndexParser(logger, _filesAggregator);

        RegisteredIds = _factory.RegisteredIds;

        Directory.CreateDirectory(_indexFolder);
    }

    public event Action? StartIndex;
    public event Action? FinishIndex;

    private readonly ILogger _logger;
    private readonly ICanPackageFactory _factory;
    private readonly string _avlLogsFolder;
    private readonly string _indexFolder;
    private readonly LogFilesAggregator _filesAggregator;
    private readonly IndexBuilder _indexBuilder;
    private readonly IndexParser _indexParser;
    private readonly Dictionary<string, LogFileScanner> _scanners = new();

    public IReadOnlySet<int> RegisteredIds { get; }
    
    public bool IsDateTimeExists(DateTime target) => _indexParser.IsDateTimeExists(target);
    public IReadOnlyList<DateTime> IndexTimes => _indexParser.IndexTimes;
    public DateTime? MinDatetime => _indexParser.MinTime;
    public DateTime? MaxDatetime => _indexParser.MaxTime;
    
    public void CreateOrLoadAllIndexes()
    {
        var indexFiles = new List<string>();
        StartIndex?.Invoke();
        foreach (var file in _filesAggregator.SortedFiles)
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
            foreach (var file in _filesAggregator.SortedFiles)
            {
                indexFiles.Add(_indexBuilder.Build(file, _indexFolder, cancellationToken));
            }
            _indexParser.LoadAll(indexFiles.ToArray());

        }, cancellationToken);

        FinishIndex?.Invoke();
    }
    
    public IEnumerable<ICanPackageParsed> GetPackages(IReadOnlySet<int> filterIds, DateTime start, DateTime end)
    {
        // var startIndex = _indexParser.FindFloor(start);
        // var endIndex = _indexParser.FindFloor(end);
        // if (startIndex is null)
        // {
        //     _logger.LogWarning("Unable to find {Start} in index.", start);
        //     throw new Exception($"Unable to find {start} in index.");
        // }
        // if (endIndex is null)
        // {
        //     _logger.LogWarning("Unable to find {End} in index.", end);
        //     throw new Exception($"Unable to find {end} in index.");
        // }
        //
        // var startFilename = startIndex.Value.Filename;
        // var endFilename = endIndex.Value.Filename;
        //
        // var context = new ParseContext();
        //
        // _logger.LogDebug(
        //     "LogParser.GetPackages(), " +
        //     "start: ({StartFilename}: {StartIndexOffset}), end: ({EndFilename}: {EndIndexOffset})",
        //     startFilename,
        //     startIndex.Value.Offset,
        //     endFilename,
        //     endIndex.Value.Offset);
        // foreach (var file in _filesAggregator.GetWrappedRange(startFilename, endFilename))
        // {
        //     var filename = Path.GetFileName(file);
        //     var scanner = GetScanner(file);
        //     
        //     var (startOffset, endOffset) = ResolveScanRange(
        //         filename, startFilename, endFilename, startIndex.Value.Offset, endIndex.Value.Offset);
        //     foreach (var (_, pkg) in scanner.GetPackagesParsed(_factory, filterIds, context, startOffset, endOffset))
        //     {
        //         yield return pkg;
        //     }
        // }

        var timeRange = new TimeRange(start, end);
        var sessions = _indexParser.Sessions.GetRange(timeRange);
        if (sessions.Count == 0)
        {
            _logger.LogDebug("LogParser.GetPackages() -> No sessions overlap [{Start}, {End}]", start, end);
            yield break;
        }
        _logger.LogDebug("GetPackages() -> found {SessionsCount} sessions", sessions.Count);

        foreach (var session in sessions)
        {
            var startIdx = _indexParser.FindFloor(session, start);
            var endIdx = _indexParser.FindCeiling(session, end);
            var context = new ParseContext();

            var reachedStartFile = false;
            foreach (var filename in session.Filenames)
            {
                if (!reachedStartFile)
                {
                    if (filename != startIdx.Filename)
                    {
                        continue;
                    }
                    reachedStartFile = true;
                }

                var isStartFile = filename == startIdx.Filename;
                var isEndFile = filename == endIdx?.Filename;
                var isLastFile = filename == session.Filenames[^1];

                var fileStart = isStartFile ? startIdx.Offset : 0L;
                var fileEnd =
                    isEndFile ? endIdx!.Value.Offset
                    : isLastFile ? (session.EndOffset ?? 0L)
                    : 0L;

                var filePath = Path.Combine(_avlLogsFolder, filename);
                var scanner = GetScanner(filePath);
                foreach (var (_, package) in scanner.GetPackagesParsed(_factory, filterIds, context, fileStart, fileEnd))
                {
                    yield return package;
                }

                if (isEndFile)
                {
                    break;
                }
            }
        }
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
    
    private static (long, long) ResolveScanRange(
        string filename,
        string startFilename,
        string endFilename,
        long startOffset,
        long endOffset)
    {
        if (startFilename == endFilename && filename == startFilename)
        {
            return (startOffset, endOffset);
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

    [GeneratedRegex(@"^[0-1][0-9]$")]
    public static partial Regex FilenameTemplateRegex();
}