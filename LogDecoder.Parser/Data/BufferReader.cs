using System.Runtime.CompilerServices;

using LogDecoder.Parser.Data.Contracts;

[assembly: InternalsVisibleTo("LogDecoder.Parser")]

namespace LogDecoder.Parser.Data;

public class BufferReader : IBufferReader
{
    public IEnumerable<LogBuffer> Read(string path)
        => Read(path, offset: 0, count: 0);
    
    public IEnumerable<LogBuffer> Read(string path, int offset)
        => Read(path, offset, count: 0);
    
    public IEnumerable<LogBuffer> Read(string path, int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read);
        
        var offsetBuffers = offset * Config.BufferSize;
        file.Seek(offsetBuffers, SeekOrigin.Begin);
        
        var totalBuffers = file.Length / Config.BufferSize;
        // TODO: Изменить. Не очевидно, что при count = 0 будет полная итерация
        var iterations = count == 0 ? totalBuffers - offset : count;
        for (var i = 0; i < iterations; i++)
        {
            var buffer = ReadNext(file);
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
}
