using LogDecoder.CAN;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;

namespace LogDecoder.Parser.Data;

public class BufferParser : Contracts.IBufferParser
{
    public List<ICanPackageParsed> GetPackagesFromBuffer(LogBuffer logBuffer, HashSet<int> filterIds)
    {
        var packages = new List<ICanPackageParsed>();
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
            
            if (!filterIds.Contains(packageId))
            {
                continue;
            }
            var package = CanPackageParser.FromBytes(rawPackage, packageType, packageLength, packageId);
            var parsedPackage = CanPackageFactory.Create(package);
            if (package.Id == 0)
            {
                Console.WriteLine($"Error while parsing package. PkgNum: {i}");
                break;
            }
            packages.Add(parsedPackage);
        }
        return packages;
    }
}