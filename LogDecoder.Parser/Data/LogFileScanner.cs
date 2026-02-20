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
    private readonly BufferParser _bufferParser = new();
    
    public IEnumerable<(int, ICanPackageParsed)> GetAllPackagesParsed(ICanPackageFactory factory, IReadOnlySet<int> filterIds, int offsetBuffers = 0, int countBuffers = 0)
    {
        using var bufferReader = new BufferReader(_file);
        var bufNum = 0;
        foreach (var buffer in bufferReader.Read(offsetBuffers, countBuffers))
        {
            foreach (var package in _bufferParser.GetPackagesFromBuffer(buffer, filterIds))
            {
                var parsed = factory.Create(package);

                if (parsed.Id == 0)
                {
                    continue;
                }
                yield return (bufNum, parsed);
            }
            bufNum++;
        }
    }

    public IEnumerable<(int, CanPackage)> GetAllPackages(HashSet<int> filterIds, int offsetBuffers = 0, int countBuffers = 0)
    {
        using var bufferReader = new BufferReader(_file);

        var bufNum = 0;
        foreach (var buffer in bufferReader.Read(offsetBuffers, countBuffers))
        {
            foreach (var package in _bufferParser.GetPackagesFromBuffer(buffer, filterIds))
            {
                yield return (bufNum, package);
            }
            bufNum++;
        }
    }
}