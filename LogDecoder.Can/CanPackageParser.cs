using System.Runtime.CompilerServices;
using OfficeOpenXml;

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
        var hrc = GetHrc(span, type, dataSize);

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
        return ReadInt32Little(raw, HeaderSize, idSize);
    }

    private static int GetHrc(ReadOnlySpan<byte> raw, PackageType type, int dataSize)
    {
        var start = HeaderSize + GetIdSize(type) + dataSize;
        return ReadInt32Little(raw, start, HrcSize);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadInt32Little(ReadOnlySpan<byte> raw, int offset, int count)
    {
         unchecked
         {
             var b0 = count > 0 ? raw[offset] : 0;
             var b1 = count > 1 ? raw[offset + 1] : 0;
             var b2 = count > 2 ? raw[offset + 2] : 0;
             var b3 = count > 3 ? raw[offset + 3] : 0;
             return b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
         }
    }
}
