using LogDecoder.CAN;

namespace LogDecoder.Parser.Data;

public class BufferParser : Contracts.IBufferParser
{
    public IEnumerable<CanPackage> GetPackagesFromBuffer(LogBuffer logBuffer, IReadOnlySet<int> filterIds)
    {
        var hasFilter = filterIds.Count != 0;
        var offset = 0;

        while (offset < logBuffer.Data.Length)
        {
            if (!CanPackageParser.TryParse(logBuffer.Data.Slice(offset), out var package))
            {
                yield break;
            }
            offset += package.Length;
            if (hasFilter && !filterIds.Contains(package.Id))
            {
                continue;
            }
            yield return package;
        }
    }
}