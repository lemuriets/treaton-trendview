using LogDecoder.CAN.Packages;

namespace LogDecoder.Parser.Data;

public static class IndexBuilder
{
    private static readonly BufferReader _bufferReader = new BufferReader();
    private static readonly BufferParser _bufferParser = new BufferParser();
    
    public static void CreateIndexFile(string logFile, string folderToSave)
    {
        Console.WriteLine($"Creating index for: {logFile}");

        var baseFilename = Path.GetFileName(logFile);
        var indexFile = Path.Combine(folderToSave, baseFilename + ".txt");
        var indexes = CreateIndex(logFile);
        
        File.WriteAllLines(indexFile, indexes);
    }

    private static List<string> CreateIndex(string logFile)
    {
        var indexes = new List<string>();
        var datetimeSet = new HashSet<string>();
        var bufNum = 0;
        foreach (var buffer in _bufferReader.Read(logFile))
        {
            foreach (var package in _bufferParser.GetPackagesFromBuffer(buffer, [IdSynchro.Id]))
            {
                var packageData = package.ParseData();
                if (packageData is null)
                {
                    continue;
                }
                var dt = packageData.Value.Messages[0];
                if (datetimeSet.Add(dt))
                {
                    indexes.Add($"{bufNum} {dt}");
                }
            }
            bufNum += 1;
        }
        return indexes;
    }
}