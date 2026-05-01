using System.Runtime.CompilerServices;
using LogDecoder.CAN;
using LogDecoder.CAN.Contracts;
using LogDecoder.Parser.Data.Contracts;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("LogDecoder.Parser")]

namespace LogDecoder.Parser.Data;

public class LogFileScanner: ILogFileScanner
{
    public LogFileScanner(ILogger logger, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        
        _logger = logger;
        _filePath = filePath;
        _tempBuffer = new byte[LogBuffer.BufferWithHeaderSize];
    }
    
    private readonly ILogger _logger;
    private readonly string _filePath;
    private readonly byte[] _tempBuffer;
    
    public IEnumerable<(long, ICanPackageParsed)> GetPackagesParsed(
        ICanPackageFactory factory,
        IReadOnlySet<int> filterIds,
        ParseContext context,
        long startOffset = 0,
        long endOffset = 0)
    {
        foreach (var (offset, package) in GetPackages(filterIds, startOffset, endOffset))
        {
            var parsed = factory.Create(package, context);
            if (parsed.Id != 0)
            {
                yield return (offset, parsed);
            }
        }
    }

    public IEnumerable<(long, CanPackage)> GetPackages(
        IReadOnlySet<int> filterIds,
        long startOffset = 0,
        long endOffset = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(endOffset);

        using var file = File.OpenRead(_filePath);

        if (endOffset == 0)
        {
            endOffset = file.Length;
        }
        
        if (startOffset > endOffset || endOffset > file.Length)
        {
            _logger.LogError("LogFileScanner.GetPackages(). startOffset={StartOffser}; endOffset={endOffset} fileLen={FileLength}",
                startOffset,
                endOffset,
                file.Length);
            throw new ArgumentException($"startOffset must be <= endOffset and endOffset <= file.Length ({file.Length})");
        }
        
        var offsetInFirstBuffer = (int)(startOffset % LogBuffer.BufferWithHeaderSize);
        var offsetInLastBuffer = (int)(endOffset % LogBuffer.BufferWithHeaderSize);
        var firstBufferIndex = (int)(startOffset / LogBuffer.BufferWithHeaderSize);
        var lastBufferIndex = (int)(endOffset / LogBuffer.BufferWithHeaderSize);

        var offsetBufferStart = startOffset - offsetInFirstBuffer;
        
        file.Seek(offsetBufferStart, SeekOrigin.Begin);
        
        for (var i = firstBufferIndex; i <= lastBufferIndex; i++)
        {
            var logBuffer = ReadBuffer(file);
            if (!logBuffer.IsValid)
            {
                yield break;
            }
            int localStartOffset = i == firstBufferIndex
                ? Math.Max(0, offsetInFirstBuffer - LogBuffer.HeaderSize)
                : 0;
            int localEndOffset = i == lastBufferIndex
                ? Math.Min(LogBuffer.PayloadSize, offsetInLastBuffer - LogBuffer.HeaderSize)
                : LogBuffer.PayloadSize;
            
            foreach (var (bufOffset, package) in logBuffer.GetPackages(filterIds, localStartOffset, localEndOffset))
            {
                var globalOffset = offsetBufferStart + (i - firstBufferIndex) * LogBuffer.BufferWithHeaderSize + bufOffset + LogBuffer.HeaderSize;
                yield return (globalOffset, package);
            }
        }
    }

    private LogBuffer ReadBuffer(FileStream file)
    {
        var read = file.Read(_tempBuffer);
        if (read != LogBuffer.BufferWithHeaderSize)
        {
            return default;
        }
        return new LogBuffer(_tempBuffer);
    }
}