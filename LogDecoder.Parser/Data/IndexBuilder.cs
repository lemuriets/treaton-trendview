using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using Microsoft.Extensions.Logging;

namespace LogDecoder.Parser.Data;

public class IndexBuilder
{
    public IndexBuilder(ILogger logger, ICanPackageFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    public const int IndexFormatVersion = 2;
    private readonly ILogger _logger;
    private readonly ICanPackageFactory _factory;
    
    public string Build(string logFile, string folderToSave)
    {
        var baseFilename = Path.GetFileName(logFile);
        var indexFilePath = Path.Combine(folderToSave, baseFilename + ".txt");
        if (File.Exists(indexFilePath) && IsActualIndexFile(indexFilePath))
        {
            return indexFilePath;
        }

        _logger.LogInformation($"Creating index for: {logFile}");

        var indexes = CreateIndex(logFile);
        var header = new IndexFileHeader(IndexFormatVersion, baseFilename);
        File.WriteAllLines(indexFilePath, [header.ToString(), ..indexes]);
    
        _logger.LogInformation($"Created log file with {indexes.Count} indexes");
        
        return indexFilePath;
    }
    
    private bool IsActualIndexFile(string indexFilePath)
    {
        try
        {
            var firstLine = File.ReadLines(indexFilePath).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstLine))
            {
                return false;
            }
            var header = new IndexFileHeader(firstLine);
            return header.FormatVersion == IndexFormatVersion;
        }
        catch
        {
            return false;
        }
    }

    private List<string> CreateIndex(string logFile)
    {
        var fileScanner = new LogFileScanner(logFile);
        
        var indexes = new List<string>();
        var seenDates = new HashSet<string>();
        var context = new ParseContext();
        foreach (var (offset, package) in fileScanner.GetPackagesParsed(_factory, new HashSet<int>{IdSynchro.Id}, context))
        {
            var packageData = package.ParseData();
            if (packageData is null)
            {
                continue;
            }
            var dt = packageData.Value.Messages[0];

            var bufNum = offset / LogBuffer.BufferWithHeaderSize;
            var offsetInBuffer = offset % LogBuffer.BufferWithHeaderSize;
            if (seenDates.Add(dt))
            {
                indexes.Add($"{bufNum} {offsetInBuffer} {dt}");
            }
        }
        return indexes;
    }
}