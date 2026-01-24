using LogDecoder.CAN.Contracts;

namespace LogDecoder.Parser.Data.Contracts;

public interface ILogFileScanner
{
    string[] GetIndex();
    
    void CreateOrLoadIndexFile();
    IEnumerable<ICanPackageParsed> ExtractAllPackages(HashSet<int> filterIds);
    IEnumerable<ICanPackageParsed> ExtractAllPackages(HashSet<int> filterIds, DateTime from, int lenghtSec);
    IEnumerable<ICanPackageParsed> ExtractAllPackages(HashSet<int> filterIds, DateTime from, DateTime to);
    ICanPackageParsed? GetFirstValidPackage(int id);
    DateTime? GetStartDatetime();
    DateTime? GetLastDatetime();
}
