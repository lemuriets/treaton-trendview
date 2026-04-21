using System.Buffers.Binary;
using LogDecoder.CAN;

namespace LogDecoder.Parser.Data;

public readonly struct LogBuffer
{
    public const int HeaderSize = 16;
    public const int PayloadSize = BufferWithHeaderSize - HeaderSize;
    public const int BufferWithHeaderSize = 32768;
    
    public readonly ReadOnlyMemory<byte> Header;
    public readonly ReadOnlyMemory<byte> Payload;
    public readonly ReadOnlyMemory<byte> Bytes;
    public readonly int PackagesCount;
    
    public bool IsValid => Header.Length == HeaderSize && Bytes.Length == BufferWithHeaderSize;

    public LogBuffer(ReadOnlyMemory<byte> buffer)
    {
        if (buffer.Length < BufferWithHeaderSize)
        {
            this = default;
            return;
        }

        Bytes = buffer;
        Header = buffer.Slice(0, HeaderSize);
        Payload = buffer.Slice(HeaderSize);
        
        PackagesCount = GetPackagesCount(Header.Span);
    }
    
    public IEnumerable<(int, CanPackage)> GetPackages(
        IReadOnlySet<int> filterIds,
        int startPayloadOffset = 0,
        int? endPayloadOffset = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startPayloadOffset);
        endPayloadOffset ??= Payload.Length;
        ArgumentOutOfRangeException.ThrowIfNegative(endPayloadOffset.Value);
        
        var offset = startPayloadOffset;
        while (offset < endPayloadOffset)
        {
            if (!CanPackageParser.TryParse(Payload.Slice(offset), out var package))
            {
                yield break;
            }
            if (filterIds.Count == 0 || filterIds.Contains(package.Id))
            {
                yield return (offset, package);
            }
            offset += package.Length;
        }
    }
    
    private int GetPackagesCount(ReadOnlySpan<byte> header)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(header);
    }
}
