using LogDecoder.CAN.Contracts;
using LogDecoder.Parser;

namespace LogDecoder.CAN.Packages;

internal record FactoryItem(string PackageName, Func<CanPackage, string, ParseContext, ICanPackageParsed> Constructor);

public class CanPackageFactory : ICanPackageFactory
{
    private readonly Dictionary<int, FactoryItem> _registered = new();
    public IReadOnlySet<int> RegisteredIds => _registered.Keys.ToHashSet();

    public CanPackageFactory()
    {
        RegisterBuiltIn();
    }

    public List<(int Id, string Name)> GetIdsWithNames()
    {
        return _registered
            .Select(kvp => (kvp.Key, kvp.Value.PackageName))
            .ToList();
    }

    public ICanPackageParsed Create(CanPackage package, ParseContext context)
    {
        if (package.Id == IdModeCivl.Id && package.Data.Length > 0)
        {
            context.CivlMode = package.Data.Span[0];
        }
        if (_registered.TryGetValue(package.Id, out var item))
        {
            return item.Constructor(package, item.PackageName, context);
        }
        return UnknownCanPackage.Instance;
    }

    public void Register(int id, string name, Func<CanPackage, string, ParseContext, ICanPackageParsed> constructor)
    {
        if (_registered.ContainsKey(id))
        {
            throw new InvalidOperationException($"Duplicate CANPackage Id: {id}");
        }

        _registered[id] = new FactoryItem(name, constructor);
    }

    private void RegisterBuiltIn()
    {
        Register(0x4A0, "Режим КИВЛ", (p, n, c) => new IdModeCivl(p, n));
        Register(0x4A9, "Ошибки калибровки КИВЛ", (p, n, c) => new IdClbrErrCivl(p, n, c.CivlMode));
        Register(0x401, "Состояние источника питания", (p, n, c) => new IdStatusPwr(p, n));
        Register(0x4C3, "Состояние ПП", (p, n, c) => new IdStatusCapno2(p, n));
        Register(0x442, "Состояние платы смесителя газов", (p, n, c) => new IdStatusMix(p, n));
        Register(0x422, "Состояние платы CAN-Ethernet", (p, n, c) => new IdStatusScm(p, n));
        Register(0x4AD, "Измеренный объём выдоха", (p, n, c) => new IdMVexpCivl(p, n));
        Register(0x4AE, "PEEP, AutoPEEP и поток на вдохе", (p, n, c) => new IdMPeepCivl(p, n));
        Register(0x4A8, "Состояние КИВЛ", (p, n, c) => new IdStatusCivl(p, n));
        Register(0x510, "Состояние КИВЛ 2", (p, n, c) => new IdStatusCivl2(p, n));
        Register(0x4AF, "Частота дыхания", (p, n, c) => new IdMRbCivl(p, n));
        Register(0x5A9, "Состояние контроллера мотора", (p, n, c) => new IdStatusMotor(p, n));
        Register(0x4B0, "Пиковое давление и стресс-индекс", (p, n, c) => new IdMPipCivl(p, n));
        Register(0x581, "Состояние Masimo 1", (p, n, c) => new IdStatusSpo_v2_1(p, n));
        Register(0x582, "Состояние Masimo 2", (p, n, c) => new IdStatusSpo_v2_2(p, n));
        Register(0x4AC, "Измеренный объём вдоха", (p, n, c) => new IdMVinspCivl(p, n));
        Register(0x4B2, "Время выдоха", (p, n, c) => new IdMTexpCivl(p, n));
        Register(0x4A6, "Графическая информация", (p, n, c) => new IdWaveCivl(p, n));
        Register(1120, "Синхро-пакет", (p, n, c) => new IdSynchro(p, n));
        Register(0x482, "Состояние метаболографа", (p, n, c) => new IdStatusCapno1(p, n));
        Register(0x4E1, "Состояния Treaton", (p, n, c) => new IdStatusSpo(p, n));
        Register(0x4B5, "Информация об утечке", (p, n, c) => new IdMLeakCivl(p, n));
        Register(0x4B3, "Время вдоха и RCinsp", (p, n, c) => new IdMTinspCivl(p, n));
        Register(0x445, "Потоки воздух/кислород", (p, n, c) => new IdFlowMix(p, n));
        Register(0x4A3, "Параметры вентиляции 1", (p, n, c) => new IdPar1Civl(p, n));
        Register(0x4A4, "Параметры вентиляции 2", (p, n, c) => new IdPar2Civl(p, n));
        Register(0x4A5, "Параметры вентиляции 3", (p, n, c) => new IdPar3Civl(p, n));
        Register(0x50E, "Параметры вентиляции 5", (p, n, c) => new IdPar5Civl2(p, n));
    }
}
