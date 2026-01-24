using System.Collections;
using System.Runtime.CompilerServices;

using LogDecoder.Parser.Data.Contracts;

[assembly: InternalsVisibleTo("LogDecoder.Parser")]

namespace LogDecoder.Parser.Data;

public class BufferReader : IBufferReader
{
    // private readonly FileStream _file;
    //
    // public BufferReader(string file)
    // {
    //     _file = new FileStream(file, FileMode.Open, FileAccess.Read);
    // }
    public IEnumerable<LogBuffer> Read(string path)
        => Read(path, offset: 0, count: 0);
    
    public IEnumerable<LogBuffer> Read(string path, int offset)
        => Read(path, offset, count: 0);
    
    public IEnumerable<LogBuffer> Read(string path, int offset, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative.");
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(nameof(path), "File path cannot be null or empty.");
        }
        
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read);
        var totalBuffers = file.Length / Config.BufferSize;
        // TODO: думаю это надо как то изменить, а то нифига не очевидно, что при count = 0 будет полная итерация
        var iterationsCount = count == 0 ? totalBuffers : count;
        for (var i = 0; i < iterationsCount; i++)
        {
            var buffer = ReadNext(file, offset);
            if (!buffer.IsValid)
            {
                yield break;
            }
            yield return buffer;
        }
    }

    private LogBuffer ReadNext(FileStream file, long offset = 0)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be non-negative.");
        }
        var buffer = new byte[Config.BufferSize];
        var offsetBuffers = offset * Config.BufferSize;
        file.Seek(offsetBuffers, SeekOrigin.Current);
        var bytesRead = file.Read(buffer, 0, buffer.Length);
        return bytesRead < Config.BufferSize
            ? new LogBuffer()
            : new LogBuffer(buffer);
    }
}
