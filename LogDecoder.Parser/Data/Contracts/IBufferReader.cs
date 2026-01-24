namespace LogDecoder.Parser.Data.Contracts;

public interface IBufferReader
{
    public IEnumerable<LogBuffer> Read(string filePath);
    public IEnumerable<LogBuffer> Read(string filePath, int offset);
    public IEnumerable<LogBuffer> Read(string filePath, int offset, int count);
}