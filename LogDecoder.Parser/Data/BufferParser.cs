using LogDecoder.CAN;

namespace LogDecoder.Parser.Data;

public class BufferParser : Contracts.IBufferParser
{
    public IEnumerable<CanPackage> GetPackagesFromBuffer(LogBuffer logBuffer, IReadOnlySet<int> filterIds)
    {
        var offset = 0;

        for (var i = 0; i < logBuffer.PackagesCount; i++)
        {
            if (offset >= logBuffer.Data.Length)
            {
                break;
            }
            if (!CanPackageParser.TryParse(logBuffer.Data.Slice(offset), out var package))
            {
                break;
            }
            offset += package.Length;
            
            if (filterIds.Count == 0 || filterIds.Contains(package.Id))
            {
                yield return package;
            }
        }
    }
}