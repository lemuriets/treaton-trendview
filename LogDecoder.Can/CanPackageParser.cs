using System.Buffers.Binary;

namespace LogDecoder.CAN;

public static class CanPackageParser
{
    private const int HeaderSize = 1;
    private const int HrcSize = 3;
    private const int MaxDataSize = 8;
    private const int MinDataSize = 0;
    
    public const int MinPackageSize = HeaderSize + HrcSize + MinDataSize + (int)PackageType.Standard;
    public const int MaxPackageSize = HeaderSize + HrcSize + MaxDataSize + (int)PackageType.Extended;

    public static bool TryParse(ReadOnlyMemory<byte> bytes, out CanPackage package)
    {
        package = default;

        var span = bytes.Span;
        
        var type = GetPackageType(span[0]);
        var dataSize = GetDataSize(span[0]);
        if (dataSize > MaxDataSize)
        {
            return false;
        }
        
        var length = GetTotalPackageLength(type, dataSize);
        if (bytes.Length < length)
        {
            return false;
        }
        
        var idSize = GetIdSize(type);
        var id = GetPackageId(span, idSize);
        var data = bytes.Slice(HeaderSize + idSize, dataSize);
        var hrc = GetHrc(span, idSize, dataSize);

        package = new CanPackage(type, id, data, hrc, length);
        
        return true;
    }
    
    private static int GetIdSize(PackageType type)
    {
        return type == PackageType.Standard ? 2 : 4;
    }

    public static int GetTotalPackageLength(PackageType type, int dataSize)
    {
        return HeaderSize + GetIdSize(type) + dataSize + HrcSize;
    }

    private static int GetDataSize(byte firstByte)
    {
        return firstByte & 0x7F;
    }

    public static PackageType GetPackageType(byte firstByte)
    {
        return firstByte < 0x80
            ? PackageType.Standard
            : PackageType.Extended;
    }

    public static int GetPackageId(ReadOnlySpan<byte> raw, int idSize)
    {
        return idSize switch
        {
            2 => BinaryPrimitives.ReadUInt16LittleEndian(raw.Slice(HeaderSize, 2)),
            4 => BinaryPrimitives.ReadInt32LittleEndian(raw.Slice(HeaderSize, 4)),
            _ => 0
        };
    }

    private static int GetHrc(ReadOnlySpan<byte> raw, int idSize, int dataSize)
    {
        var start = HeaderSize + idSize + dataSize;
        return raw[start]
               | (raw[start + 1] << 8)
               | (raw[start + 2] << 16);
    }
}
