using System.Buffers.Binary;
using LogDecoder.Parser.Data;

namespace LogDecoder.Parser.Tests.Fixtures;

internal static class LogFixture
{
    public const int SynchroId = 1120;
    public const int OxyId = 1123;
    public const byte PaddingByte = 0xFF;

    public static byte[] SynchroPackage(DateTime time)
    {
        var pkg = new byte[12];
        pkg[0] = 0x06;
        WriteStandardId(pkg, SynchroId);
        pkg[3] = (byte)(time.Year - 2000);
        pkg[4] = (byte)time.Month;
        pkg[5] = (byte)time.Day;
        pkg[6] = (byte)time.Hour;
        pkg[7] = (byte)time.Minute;
        pkg[8] = (byte)time.Second;
        return pkg;
    }

    public static byte[] OxyPackage(byte data1 = 0, byte data2 = 0)
    {
        var pkg = new byte[8];
        pkg[0] = 0x02;
        WriteStandardId(pkg, OxyId);
        pkg[3] = data1;
        pkg[4] = data2;
        return pkg;
    }

    public static byte[] Buffer(params byte[][] packages)
    {
        return Buffer((IReadOnlyList<byte[]>)packages);
    }

    public static byte[] Buffer(IReadOnlyList<byte[]> packages)
    {
        var buffer = new byte[LogBuffer.BufferWithHeaderSize];
        buffer.AsSpan().Fill(PaddingByte);

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(0, 4), packages.Count);

        var offset = LogBuffer.HeaderSize;
        foreach (var pkg in packages)
        {
            pkg.CopyTo(buffer.AsSpan(offset));
            offset += pkg.Length;
        }
        return buffer;
    }

    public static void WriteFile(string filePath, params byte[][] buffers)
    {
        using var stream = File.Create(filePath);
        foreach (var buffer in buffers)
        {
            stream.Write(buffer);
        }
    }

    private static void WriteStandardId(byte[] pkg, int id)
    {
        pkg[1] = (byte)id;
        pkg[2] = (byte)(id >> 8);
    }
}
