using System.Runtime.CompilerServices;
namespace LogDecoder.CAN;

public static class CanPackageParser
{
    private const int LengthSize = 1;
    private const int HrcSize = 3;
    private const int MaxDataSize = 8;
    
    public const int MaxPackageSize = LengthSize + HrcSize + MaxDataSize + (int)PackageType.Extended;

    public static CanPackage FromBytes(byte[] rawPackage)
    {
        var type = GetPackageType(rawPackage[0]);
        var length = GetPackageLength(rawPackage[0], type);
        var id = GetPackageId(rawPackage, type);
        var data = GetData(rawPackage, type);
        if (data.Length == 0)
        {
            return new CanPackage();
        }
        var hrc = GetHrc(rawPackage, type, data.Length);

        return new CanPackage(type, id, data, hrc, length);
    }

    public static CanPackage FromBytes(byte[] rawPackage, PackageType type, int length, int id)
    {
        var data = GetData(rawPackage, type);
        if (data.Length == 0)
        {
            return new CanPackage();
        }
        if (length > rawPackage.Length)
        {
            return new CanPackage();
        }
        var hrc = GetHrc(rawPackage, type, data.Length);

        return new CanPackage(type, id, data, hrc, length);
    }

    public static int GetPackageLength(byte firstByte, PackageType type)
    {
        var dataLength = GetDataSize(firstByte);
        return LengthSize + (int)type + dataLength + HrcSize;
    }

    private static int GetDataSize(byte firstByte) => firstByte & 0x7F;

    public static PackageType GetPackageType(byte firstByte)
    {
        return (firstByte & 0xFF) < 0x80 ? PackageType.Standard : PackageType.Extended;
    }

    public static int GetPackageId(byte[] raw, PackageType type)
    {
        return ReadInt32Little(raw, LengthSize, (int)type);
    }

    private static byte[] GetData(byte[] raw, PackageType type)
    {
        var size = GetDataSize(raw[0]);
        if (size > MaxDataSize)
        {
            return [];
        }

        var start = LengthSize + (int)type;
        return raw[start..(start + size)];
    }

    private static int GetHrc(byte[] raw, PackageType type, int dataSize)
    {
        var start = LengthSize + (int)type + dataSize;
        return ReadInt32Little(raw, start, HrcSize);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadInt32Little(byte[] raw, int offset, int count)
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
