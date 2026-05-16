namespace LogDecoder.Parser.Data.Contracts;

public interface IIndexBuilder
{
    string Build(string logFile, string folderToSave);
}

public interface IIndexParser
{
    LogSessionsSorted Sessions { get; }
    DateTime? MinTime { get; }
    DateTime? MaxTime { get; }

    bool IsDateTimeExists(DateTime target);

    void LoadAll(string[] indexFiles);

    IndexEntry? FindFloor(DateTime target);
}
