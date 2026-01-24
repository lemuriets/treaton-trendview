using LogDecoder.CAN.Contracts;

namespace LogDecoder.Parser.Data.Contracts;

public interface IBufferParser
{
    List<ICanPackageParsed> GetPackagesFromBuffer(LogBuffer logBuffer, HashSet<int> filterIds);
}
