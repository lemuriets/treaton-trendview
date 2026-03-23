using LogDecoder.CAN;

namespace LogDecoder.Parser.Data;

public class BufferParser : Contracts.IBufferParser
{
    public IEnumerable<CanPackage> GetPackagesFromBuffer(LogBuffer logBuffer, IReadOnlySet<int> filterIds)
    {
        var hasFilter = filterIds.Count != 0;
        var offset = 0;

        for (var i = 0; i < logBuffer.PackagesCount; i++)
        {
            if (offset >= logBuffer.Data.Length)
            {
                break;
            }
            if (!CanPackageParser.TryParse(logBuffer.Data.Slice(offset), out var package))
            {
                yield break;
            }
            if (hasFilter && !filterIds.Contains(package.Id))
            {
                continue;
            }
            offset += package.Length;
            yield return package;
        }
    }
}