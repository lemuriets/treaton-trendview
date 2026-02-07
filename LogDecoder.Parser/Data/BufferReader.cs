using System.Runtime.CompilerServices;

using LogDecoder.Parser.Data.Contracts;

[assembly: InternalsVisibleTo("LogDecoder.Parser")]

namespace LogDecoder.Parser.Data;

public class BufferReader : IBufferReader, IDisposable
{
    public BufferReader(string file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        
        _file = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
    }
    
    private readonly FileStream _file;
    
    public IEnumerable<LogBuffer> Read()
        => Read(offset: 0, count: 0);
    
    public IEnumerable<LogBuffer> Read(int offset)
        => Read(offset, count: 0);
    
    public IEnumerable<LogBuffer> Read(int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        
        var offsetBuffers = offset * Config.BufferSize;
        _file.Seek(offsetBuffers, SeekOrigin.Begin);
        
        var totalBuffers = _file.Length / Config.BufferSize;
        // TODO: Изменить. Не очевидно, что при count = 0 будет полная итерация
        var iterations = count == 0 ? totalBuffers - offset : count;
        for (var i = 0; i < iterations; i++)
        {
            var buffer = ReadNext(_file);
            if (!buffer.IsValid)
            {
                yield break;
            }
            yield return buffer;
        }
    }

    private LogBuffer ReadNext(FileStream file)
    {
        var buffer = new byte[Config.BufferSize];
        var bytesRead = file.Read(buffer, 0, buffer.Length);
        return bytesRead < Config.BufferSize
            ? new LogBuffer()
            : new LogBuffer(buffer);
    }

    public void Dispose()
    {
        _file.Dispose();
    }
}
