using LogDecoder.CAN;
using LogDecoder.CAN.Contracts;

namespace LogDecoder.Parser.Data.Contracts;

public interface ILogFileScanner
{
    IEnumerable<(long Offset, CanPackage Package)> GetPackages(
        IReadOnlySet<int> filterIds,
        long startOffset = 0,
        long endOffset = 0);

    IEnumerable<(long Offset, ICanPackageParsed Package)> GetPackagesParsed(
        ICanPackageFactory factory,
        IReadOnlySet<int> filterIds,
        ParseContext context,
        long startOffset = 0,
        long endOffset = 0);
}
