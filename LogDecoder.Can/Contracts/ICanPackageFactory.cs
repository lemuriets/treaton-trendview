namespace LogDecoder.CAN.Contracts;

public interface ICanPackageFactory
{
    ICanPackageParsed Create(CanPackage package);
    void Register(int id, string name, Func<CanPackage, string, ICanPackageParsed> constructor);
    IReadOnlySet<int> RegisteredIds { get; }
}