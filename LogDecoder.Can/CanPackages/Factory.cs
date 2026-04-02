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
        Register(0x4A0, "ID_MODE_CIVL", (p, n, c) => new IdModeCivl(p, n));
        Register(0x4A9, "ID_CLBR_ERR_CIVL", (p, n, c) => new IdClbrErrCivl(p, n, c.CivlMode));
        Register(0x401, "ID_STATUS_PWR", (p, n, c) => new IdStatusPwr(p, n));
        Register(0x4C3, "ID_STATUS_CAPNO2", (p, n, c) => new IdStatusCapno2(p, n));
        Register(0x442, "ID_STATUS_MIX", (p, n, c) => new IdStatusMix(p, n));
        Register(0x422, "ID_STATUS_SCM", (p, n, c) => new IdStatusScm(p, n));
        Register(0x4AD, "ID_M_VEXP_CIVL", (p, n, c) => new IdMVexpCivl(p, n));
        Register(0x4AE, "ID_M_PEEP_CIVL", (p, n, c) => new IdMPeepCivl(p, n));
        Register(0x4A8, "ID_STATUS_CIVL", (p, n, c) => new IdStatusCivl(p, n));
        Register(0x510, "ID_STATUS2_CIVL2", (p, n, c) => new IdStatusCivl2(p, n));
        Register(0x4AF, "ID_M_RB_CIVL", (p, n, c) => new IdMRbCivl(p, n));
        Register(0x5A9, "ID_STATUS_MOTOR", (p, n, c) => new IdStatusMotor(p, n));
        Register(0x4B0, "ID_M_PIP_CIVL", (p, n, c) => new IdMPipCivl(p, n));
        Register(0x581, "ID_STATUS_SPO_V2_1", (p, n, c) => new IdStatusSpo_v2_1(p, n));
        Register(0x582, "ID_STATUS_SPO_V2_2", (p, n, c) => new IdStatusSpo_v2_2(p, n));
        Register(0x4AC, "ID_M_VINSP_CIVL", (p, n, c) => new IdMVinspCivl(p, n));
        Register(0x4B2, "ID_M_TEXP_CIVL", (p, n, c) => new IdMTexpCivl(p, n));
        Register(0x4A6, "ID_WAVE_CIVL", (p, n, c) => new IdWaveCivl(p, n));
        Register(0x460, "ID_SYNHRO", (p, n, c) => new IdSynchro(p, n));
        Register(0x482, "ID_STATUS_CAPNO1", (p, n, c) => new IdStatusCapno1(p, n));
        Register(0x4E1, "ID_STATUS_SPO", (p, n, c) => new IdStatusSpo(p, n));
        Register(0x4B5, "ID_M_LEAK_CIVL", (p, n, c) => new IdMLeakCivl(p, n));
        Register(0x4B3, "ID_M_TINSP_CIVL", (p, n, c) => new IdMTinspCivl(p, n));
        Register(0x445, "ID_FLOW_MIX", (p, n, c) => new IdFlowMix(p, n));
        Register(0x4A3, "ID_PAR1_CIVL", (p, n, c) => new IdPar1Civl(p, n));
        Register(0x4A4, "ID_PAR2_CIVL", (p, n, c) => new IdPar2Civl(p, n));
        Register(0x4A5, "ID_PAR3_CIVL", (p, n, c) => new IdPar3Civl(p, n));
        Register(0x50E, "ID_PAR5_CIVL2", (p, n, c) => new IdPar5Civl2(p, n));
    }
}
