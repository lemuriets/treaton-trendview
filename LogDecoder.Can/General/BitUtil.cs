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
    public static ushort ToU16(byte b0, byte b1) => (ushort)(b0 | (b1 << 8));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToU32(byte b0, byte b1, byte b2, byte b3) => (uint)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ToU64(params byte[] bytes)
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
        unchecked((short)(b0 | (b1 << 8)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToS32(byte b0, byte b1, byte b2, byte b3) =>
        unchecked((int)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToS64(params byte[] bytes)
    {
        var len = bytes.Length > 8 ? 8 : bytes.Length;
        long bits = 0;
        for (var i = 0; i < len; i++)
            bits |= (long)bytes[i] << (8 * i);
        return bits;
    }
}