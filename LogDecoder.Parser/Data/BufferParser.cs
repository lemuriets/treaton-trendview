using LogDecoder.CAN;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;

namespace LogDecoder.Parser.Data;

public class BufferParser : Contracts.IBufferParser
{
    public CanPackage GetFirstPackage(LogBuffer buffer, int id)
    {
        return GetPackagesFromBuffer(buffer, [id]).FirstOrDefault();
    }
    
    public List<CanPackage> GetPackagesFromBuffer(LogBuffer logBuffer, HashSet<int> filterIds)
    {
        var packages = new List<CanPackage>();
        var offset = 0;

        for (var i = 0; i < logBuffer.PackagesCount; i++)
        {
            if (offset >= logBuffer.Data.Length)
            {
                break;
            }
            var firstByte = logBuffer.Data[offset];
            
            var packageType = CanPackageParser.GetPackageType(firstByte);
            var packageLength = CanPackageParser.GetPackageLength(firstByte, packageType);
            if (packageLength > CanPackageParser.MaxPackageSize)
            {
                break;
            }
            var rawPackage = new byte[packageLength];
            Buffer.BlockCopy(logBuffer.Data, offset, rawPackage, 0, packageLength);
            var packageId = CanPackageParser.GetPackageId(rawPackage, packageType);
            
            offset += packageLength;
            
            if (!filterIds.Contains(packageId) && filterIds.Count != 0)
            {
                continue;
            }
            var package = CanPackageParser.FromBytes(rawPackage, packageType, packageLength, packageId);
            packages.Add(package);
        }
        return packages;
    }
}