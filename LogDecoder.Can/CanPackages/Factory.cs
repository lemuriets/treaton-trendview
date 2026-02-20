using LogDecoder.CAN.Contracts;

namespace LogDecoder.CAN.Packages;

internal record FactoryItem(string PackageName, Func<CanPackage, string, ICanPackageParsed> Constructor);

public class CanPackageFactory : ICanPackageFactory
{
    private readonly Dictionary<int, FactoryItem> _registered = new();
    public IReadOnlySet<int> RegisteredIds  =>  _registered.Keys.ToHashSet();

    public CanPackageFactory()
    {
        RegisterBuiltIn();
    }
    
    public ICanPackageParsed Create(CanPackage package)
    {
        if (_registered.TryGetValue(package.Id, out var item))
        {
            return item.Constructor(package, item.PackageName);
        }
        return  UnknownCanPackage.Instance;
    }

    public void Register(int id, string name, Func<CanPackage, string, ICanPackageParsed> constructor)
    {
        if (_registered.ContainsKey(id))
        {
            throw new InvalidOperationException($"Duplicate CANPackage Id: {id}");
        }
        _registered[id] = new FactoryItem(name, constructor);
    }

    private void RegisterBuiltIn()
    {
        Register(0x401, "Состояние ИП", (p, n) => new IdStatusPwr(p, n));
        Register(0x4B1, "Минутная вентиляция", (p, n) => new IdMMvCivl(p, n));
        Register(0x4C3, "Состояние ПП", (p, n) => new IdStatusCapno2(p, n));
        Register(0x442, "Состояние платы СГ", (p, n) => new IdStatusMix(p, n));
        Register(0x422, "Состояние платы CAN-Ethernet", (p, n) => new IdStatusScm(p, n));
        Register(0x4AD, "Измеренный объём выдоха", (p, n) => new IdMVexpCivl(p, n));
        Register(0x4AE, "PEEP, AutoPEEP и поток на вдохе", (p, n) => new IdMPeepCivl(p, n));
        Register(0x4A8, "Состояние КИВЛ", (p, n) => new IdStatusCivl(p, n));
        Register(0x4AF, "Частота дыхания", (p, n) => new IdMRbCivl(p, n));
        Register(0x5A9, "Состояние КМ", (p, n) => new IdStatusMotor(p, n));
        Register(0x4B0, "Пиковое давление и стресс-индекс", (p, n) => new IdMPipCivl(p, n));
        Register(0x581, "Состояние Masimo 1", (p, n) => new IdStatusSpo_v2_1(p, n));
        Register(0x582, "Состояние Masimo 2", (p, n) => new IdStatusSpo_v2_2(p, n));
        Register(0x4AC, "Измеренный объём вдоха", (p, n) => new IdMVinspCivl(p, n));
        Register(0x4B2, "Время выдоха", (p, n) => new IdMTexpCivl(p, n));
        Register(0x4A6, "Графическая информация", (p, n) => new IdWaveCivl(p, n));
        Register(1120, "Синхро-пакет", (p, n) => new IdSynchro(p, n));
        Register(0x482, "Состояние метаболографа", (p, n) => new IdStatusCapno1(p, n));
        Register(0x4E1, "Состояния Treaton", (p, n) => new IdStatusSpo(p, n));
        Register(0x4B4, "Комплайнс и резистанс", (p, n) => new IdMComplCivl(p, n));
        Register(0x4B5, "Информация об утечке", (p, n) => new IdMLeakCivl(p, n));
        Register(0x4B3, "Время вдоха и RCinsp", (p, n) => new IdMTinspCivl(p, n));
    }
}

