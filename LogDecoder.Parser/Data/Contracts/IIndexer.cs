namespace LogDecoder.Parser.Data.Contracts;

public interface IIndexer
{
    void Load(string indexFile);
    string[] GetIndex(string indexFile);
    void CreateIndexFile(string file, string saveTo);
    int FindBufferByDateTime(string indexFile, DateTime target);
    int FindNearestBufferByDateTime(string indexFile, DateTime target);
    DateTime? GetLastDatetime(string file);
}