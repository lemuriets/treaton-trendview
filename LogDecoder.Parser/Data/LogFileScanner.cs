using System.Runtime.CompilerServices;
using LogDecoder.CAN;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using LogDecoder.Parser.Data.Contracts;

[assembly: InternalsVisibleTo("LogDecoder.Parser")]

namespace LogDecoder.Parser.Data;

internal class LogFileScanner: ILogFileScanner
{
    public LogFileScanner(string file)
    {
        _file = file;
    }

    private readonly string _file;

    public (int, ICanPackageParsed?) GetFirstValidPackage(int id)
    {
        foreach (var (bufNum, package) in ExtractAllPackages([id]))
        {
            var parsedPackage = CanPackageFactory.Create(package);
            if (parsedPackage.Id != 0)
            {
                return (bufNum, parsedPackage);
            }
        }
        return (-1, null);
    }
    
    public IEnumerable<(int, CanPackage)> ExtractAllPackages(HashSet<int> filterIds)
    {
        return ExtractAllPackages(filterIds, 0, 0);
    }
    
    public IEnumerable<(int, CanPackage)> ExtractAllPackages(HashSet<int> filterIds, int offsetBuffers)
    {
        return ExtractAllPackages(filterIds, offsetBuffers, 0);
    }
    
    public IEnumerable<(int, CanPackage)> ExtractAllPackages(HashSet<int> filterIds, int offsetBuffers, int countBuffers)
    {
        var bufferReader = new BufferReader(_file);
        var bufferParser = new BufferParser();

        var bufNum = 0;
        foreach (var buffer in bufferReader.Read(offsetBuffers, countBuffers))
        {
            foreach (var package in bufferParser.GetPackagesFromBuffer(buffer, filterIds))
            {
                yield return (bufNum, package);
            }
            bufNum++;
        }
    }
}