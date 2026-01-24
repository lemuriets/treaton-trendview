namespace LogDecoder.CAN;

public readonly struct CanPackage(PackageType type, int id, byte[] data, int hrc, int length)
{
    public PackageType Type { get; } = type;
    public int Id { get; } = id;
    public byte[] Data { get; } = data;
    public int Hrc { get; } = hrc;
    public int Length { get; } = length;
}