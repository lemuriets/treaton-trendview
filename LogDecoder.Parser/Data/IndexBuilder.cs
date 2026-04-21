using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;

namespace LogDecoder.Parser.Data;

public class IndexBuilder
{
    public IndexBuilder(ICanPackageFactory factory)
    {
        _factory = factory;
    }

    private readonly ICanPackageFactory _factory;
    
    public string CreateIndexFile(string logFile, string folderToSave, bool rewrite = false)
    {
        var baseFilename = Path.GetFileName(logFile);
        var indexFilePath = Path.Combine(folderToSave, baseFilename + ".txt");
        if (File.Exists(indexFilePath) && !rewrite)
        {
            return indexFilePath;
        }
        Console.WriteLine($"[INFO] Creating index for: {logFile}");
        
        var indexes = CreateIndex(logFile);
        File.WriteAllLines(indexFilePath, indexes);
        return indexFilePath;
    }

    private List<string> CreateIndex(string logFile)
    {
        var fileScanner = new LogFileScanner(logFile);
        
        var indexes = new List<string>();
        var datetimeSet = new HashSet<string>();
        var context = new ParseContext();
        foreach (var (offset, package) in fileScanner.GetPackagesParsed(_factory, new HashSet<int>{IdSynchro.Id}, context))
        {
            var packageData = package.ParseData();
            if (packageData is null)
            {
                continue;
            }
            var dt = packageData.Value.Messages[0];
            if (datetimeSet.Add(dt))
            {
                indexes.Add($"{offset} {dt}");
            }
        }
        return indexes;
    }
}