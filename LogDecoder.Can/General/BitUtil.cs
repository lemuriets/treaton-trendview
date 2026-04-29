using System.Runtime.CompilerServices;

namespace LogDecoder.CAN.General;

internal static class BitUtil
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBitSet(int value, int bit) => ((value >> bit) & 1) == 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBitSet(uint value, int bit) => ((value >> bit) & 1) == 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBitSet(ulong value, int bit) => ((value >> bit) & 1) == 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBitSet(byte value, int bit) => ((value >> bit) & 1) == 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ToU16(byte b0, byte b1) =>
        (ushort)(b0 | (b1 << 8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToU32(byte b0, byte b1, byte b2, byte b3) =>
        (uint)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24));

    public static ulong ToU64(ReadOnlySpan<byte> bytes)
    {
        var len = bytes.Length > 8 ? 8 : bytes.Length;
        ulong bits = 0;

        for (var i = 0; i < len; i++)
        {
            bits |= (ulong)bytes[i] << (8 * i);
        }

        return bits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ToS16(byte b0, byte b1) =>
        unchecked((short)ToU16(b0, b1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToS32(byte b0, byte b1, byte b2, byte b3) =>
        unchecked((int)ToU32(b0, b1, b2, b3));

    public static long ToS64(ReadOnlySpan<byte> bytes)
    {
        return unchecked((long)ToU64(bytes));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ExtractBits(byte value, int fromBit, int toBit)
    {
        return (byte)ExtractBits((ulong)value, fromBit, toBit, 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ExtractBits(ushort value, int fromBit, int toBit)
    {
        return (ushort)ExtractBits(value, fromBit, toBit, 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ExtractBits(uint value, int fromBit, int toBit)
    {
        return (uint)ExtractBits(value, fromBit, toBit, 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ExtractBits(ulong value, int fromBit, int toBit)
    {
        return ExtractBits(value, fromBit, toBit, 64);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ExtractBits(ulong value, int fromBit, int toBit, int maxBits)
    {
        ValidateBitRange(fromBit, toBit, maxBits);

        var bitCount = toBit - fromBit + 1;

        if (bitCount == 64)
        {
            return value;
        }

        var mask = (1UL << bitCount) - 1;

        return (value >> fromBit) & mask;
    }

    private static void ValidateBitRange(int fromBit, int toBit, int maxBits)
    {
        if (fromBit < 0 || fromBit >= maxBits)
        {
            throw new ArgumentOutOfRangeException(nameof(fromBit));
        }

        if (toBit < 0 || toBit >= maxBits)
        {
            throw new ArgumentOutOfRangeException(nameof(toBit));
        }

        if (fromBit > toBit)
        {
            throw new ArgumentException("fromBit must be less than or equal to toBit.");
        }
    }
}