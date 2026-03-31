using LogDecoder.Parser;

namespace LogDecoder.CAN.Contracts;

public interface ICanPackageFactory
{
    ICanPackageParsed Create(CanPackage package, ParseContext context);
    void Register(int id, string name, Func<CanPackage, string, ParseContext, ICanPackageParsed> constructor);
    IReadOnlySet<int> RegisteredIds { get; }
    List<(int Id, string Name)> GetIdsWithNames();
}