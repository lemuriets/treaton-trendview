using System.Runtime.CompilerServices;

using LogDecoder.Parser.Data.Contracts;

[assembly: InternalsVisibleTo("LogDecoder.Parser")]

namespace LogDecoder.Parser.Data;

public class BufferReader : IBufferReader, IDisposable
{
    public BufferReader(string file, int bufferSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        
        _file = new FileStream(
            file,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            FileOptions.SequentialScan);
        _buffer = new byte[_bufferSize];
        _bufferSize = bufferSize;
    }
    
    private readonly FileStream _file;
    private readonly byte[] _buffer;
    private readonly int _bufferSize;
    
    public IEnumerable<LogBuffer> Read(int offset = 0, int count = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        
        var offsetBuffers = offset * _bufferSize;
        _file.Seek(offsetBuffers, SeekOrigin.Begin);
        
        var totalBuffers = _file.Length / _bufferSize;
        // TODO: Изменить. Не очевидно, что при count = 0 будет полная итерация
        var iterations = count == 0 ? totalBuffers - offset : count;
        for (long i = 0; i < iterations; i++)
        {
            var read = _file.Read(_buffer);
            if (read < _bufferSize)
            {
                yield break;
            }
            yield return new LogBuffer(_buffer);
        }
    }

    public void Dispose()
    {
        _file.Dispose();
    }
}
