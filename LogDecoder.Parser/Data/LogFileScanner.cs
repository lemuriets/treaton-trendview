using System.Runtime.CompilerServices;
using LogDecoder.CAN;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using LogDecoder.Parser.Data.Contracts;

[assembly: InternalsVisibleTo("LogDecoder.Parser")]

namespace LogDecoder.Parser.Data;

public class LogFileScanner: ILogFileScanner
{
    public LogFileScanner(string file)
    {
        _file = file;
    }

    private readonly string _file;
    private readonly BufferParser _bufferParser = new();
    
    public IEnumerable<(int, ICanPackageParsed)> GetAllPackagesParsed(ICanPackageFactory factory, IReadOnlySet<int> filterIds, ParseContext context, int offsetBuffers = 0, int countBuffers = 0)
    {
        foreach (var (bufNum, package) in GetAllPackages(filterIds, offsetBuffers, countBuffers))
        {
            var parsed = factory.Create(package, context);
            if (parsed.Id != 0)
            {
                yield return (bufNum, parsed);
            }
        }
    }

    public IEnumerable<(int, CanPackage)> GetAllPackages(IReadOnlySet<int> filterIds, int offsetBuffers = 0, int countBuffers = 0)
    {
        using var bufferReader = new BufferReader(_file, Config.BufferSize);
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