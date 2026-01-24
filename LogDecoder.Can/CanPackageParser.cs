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

// ОПТИМИЗАЦИЯ ОТ ГПТ

// using System.Runtime.CompilerServices;
//
// namespace LogDecoder.CAN;
//
// public static class CanPackageParser
// {
//     private const int LengthSize = 1;
//     private const int HrcSize = 3;
//     private const int MaxDataSize = 8;
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public static CanPackage? FromBytes(byte[] rawPackage)
//     {
//         if (rawPackage.Length == 0 || rawPackage.Length < LengthSize + 1)
//             return null;
//
//         var first = rawPackage[0];
//         var type = GetPackageType(first);
//         var dataSize = GetDataSize(first);
//         if (dataSize == 0 || dataSize > MaxDataSize)
//             return null;
//
//         var idLen = (int)type;
//         var length = LengthSize + idLen + dataSize + HrcSize;
//         if (rawPackage.Length < length)
//             return null;
//
//         var id = ReadInt32Little(rawPackage, LengthSize, idLen);
//
//         var data = new byte[dataSize];
//         if (dataSize > 0)
//             Buffer.BlockCopy(rawPackage, LengthSize + idLen, data, 0, dataSize);
//
//         var hrc = ReadInt32Little(rawPackage, LengthSize + idLen + dataSize, HrcSize);
//
//         return new CanPackage(type, id, data, hrc, length);
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public static CanPackage? FromBytes(byte[] rawPackage, PackageType type, int length, int id)
//     {
//         if (rawPackage.Length == 0)
//             return null;
//
//         if (rawPackage.Length < LengthSize + (int)type)
//             return null;
//
//         var dataSize = GetDataSize(rawPackage[0]);
//         if (dataSize == 0 || dataSize > MaxDataSize)
//             return null;
//
//         if (rawPackage.Length < length)
//             return null;
//
//         var data = new byte[dataSize];
//         if (dataSize > 0)
//             Buffer.BlockCopy(rawPackage, LengthSize + (int)type, data, 0, dataSize);
//
//         var hrc = ReadInt32Little(rawPackage, LengthSize + (int)type + dataSize, HrcSize);
//
//         return new CanPackage(type, id, data, hrc, length);
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public static int GetPackageLength(byte firstByte, PackageType type)
//     {
//         return LengthSize + (int)type + GetDataSize(firstByte) + HrcSize;
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     private static int GetDataSize(byte firstByte) => firstByte & 0x7F;
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public static PackageType GetPackageType(byte firstByte) => firstByte < 0x80 ? PackageType.Standard : PackageType.Extended;
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public static int GetPackageId(byte[] raw, PackageType type)
//     {
//         return ReadInt32Little(raw, LengthSize, (int)type);
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     private static byte[] GetData(byte[] raw, PackageType type)
//     {
//         var size = GetDataSize(raw[0]);
//         if (size == 0 || size > MaxDataSize)
//             return Array.Empty<byte>();
//
//         var start = LengthSize + (int)type;
//         var data = new byte[size];
//         Buffer.BlockCopy(raw, start, data, 0, size);
//         return data;
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     private static int GetHrc(byte[] raw, PackageType type, int dataSize)
//     {
//         var start = LengthSize + (int)type + dataSize;
//         return ReadInt32Little(raw, start, HrcSize);
//     }
//
//     // Читает count байт (1..4) в little-endian порядок и возвращает int.
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     private static int ReadInt32Little(byte[] raw, int offset, int count)
//     {
//         unchecked
//         {
//             var b0 = count > 0 ? raw[offset] : 0;
//             var b1 = count > 1 ? raw[offset + 1] : 0;
//             var b2 = count > 2 ? raw[offset + 2] : 0;
//             var b3 = count > 3 ? raw[offset + 3] : 0;
//             return b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
//         }
//     }
// }
