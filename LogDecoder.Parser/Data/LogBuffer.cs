using System.Buffers.Binary;

namespace LogDecoder.Parser.Data;

public readonly struct LogBuffer
{
    private const int HeaderSize = 16;
    
    public readonly ReadOnlyMemory<byte> Header;
    public readonly ReadOnlyMemory<byte> Data;
    public readonly int PackagesCount;
    
    public bool IsValid => Header.Length == HeaderSize;
    public bool IsEmpty => Data.Length == 0;

    public LogBuffer(ReadOnlyMemory<byte> buffer)
    {
        if (buffer.Length < HeaderSize)
        {
            Header = default;
            Data = default;
            PackagesCount = 0;
            return;
        }
        
        Header = buffer.Slice(0, HeaderSize);
        Data = buffer.Slice(HeaderSize);
        
        PackagesCount = GetPackagesCount(Header.Span);
    }

    private int GetPackagesCount(ReadOnlySpan<byte> header)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(header);
    }
}
