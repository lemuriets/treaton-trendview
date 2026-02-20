using LogDecoder.CAN;

namespace LogDecoder.Parser.Data;

// TODO: Сделать нормальный DI
public static class Config
{
    public const int BufferSize = 32768;
    public const int MinSessionIntervalSeconds = 20;
    public const int MinBufferPackagesCountWhenFilled = BufferSize / CanPackageParser.MaxPackageSize;
}
