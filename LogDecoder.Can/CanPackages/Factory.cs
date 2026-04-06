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

    public List<(int Id, string Name)> GetIdsWithNames(IReadOnlySet<int>? excludeIds = null)
    {
        return _registered
            .Where(kvp => excludeIds == null || !excludeIds.Contains(kvp.Key))
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
    Register(1023, "ID_OFF_PWR", (p, n, c) => new IdOffPwr(p, n));
    Register(1025, "ID_STATUS_PWR", (p, n, c) => new IdStatusPwr(p, n));
    Register(1058, "ID_STATUS_SCM", (p, n, c) => new IdStatusScm(p, n));
    Register(1090, "ID_STATUS_MIX", (p, n, c) => new IdStatusMix(p, n));
    Register(1093, "ID_FLOW_MIX", (p, n, c) => new IdFlowMix(p, n));
    Register(1120, "ID_SYNHRO", (p, n, c) => new IdSynchro(p, n));
    Register(1123, "ID_OXY", (p, n, c) => new IdOxy(p, n));
    Register(1154, "ID_STATUS_CAPNO1", (p, n, c) => new IdStatusCapno1(p, n));
    Register(1156, "ID_WAVE_CAPNO1", (p, n, c) => new IdWaveCapno1(p, n));
    Register(1157, "ID_CO2_DATA_CAPNO1", (p, n, c) => new IdCo2DataCapno1(p, n));
    Register(1168, "ID_METABOL_DATA_CAPNO1", (p, n, c) => new IdMetabolDataCapno1(p, n));
    Register(1184, "ID_MODE_CIVL", (p, n, c) => new IdModeCivl(p, n));
    Register(1187, "ID_PAR1_CIVL", (p, n, c) => new IdPar1Civl(p, n));
    Register(1188, "ID_PAR2_CIVL", (p, n, c) => new IdPar2Civl(p, n));
    Register(1189, "ID_PAR3_CIVL", (p, n, c) => new IdPar3Civl(p, n));
    Register(1190, "ID_WAVE_CIVL", (p, n, c) => new IdWaveCivl(p, n));
    Register(1192, "ID_STATUS_CIVL", (p, n, c) => new IdStatusCivl(p, n));
    Register(1193, "ID_CLBR_ERR_CIVL", (p, n, c) => new IdClbrErrCivl(p, n, c.CivlMode));
    Register(1196, "ID_M_VINSP_CIVL", (p, n, c) => new IdMVinspCivl(p, n));
    Register(1197, "ID_M_VEXP_CIVL", (p, n, c) => new IdMVexpCivl(p, n));
    Register(1198, "ID_M_PEEP_CIVL", (p, n, c) => new IdMPeepCivl(p, n));
    Register(1199, "ID_M_RB_CIVL", (p, n, c) => new IdMRbCivl(p, n));
    Register(1200, "ID_M_PIP_CIVL", (p, n, c) => new IdMPipCivl(p, n));
    Register(1201, "ID_M_MV_CIVL", (p, n, c) => new IdMMvCivl(p, n));
    Register(1202, "ID_M_TEXP_CIVL", (p, n, c) => new IdMTexpCivl(p, n));
    Register(1203, "ID_M_TINSP_CIVL", (p, n, c) => new IdMTinspCivl(p, n));
    Register(1204, "ID_M_COMPL_CIVL", (p, n, c) => new IdMComplCivl(p, n));
    Register(1205, "ID_M_LEAK_CIVL", (p, n, c) => new IdMLeakCivl(p, n));
    Register(1207, "ID_WORKTIME_CIVL", (p, n, c) => new IdWorktimeCivl(p, n));
    Register(1219, "ID_STATUS_CAPNO2", (p, n, c) => new IdStatusCapno2(p, n));
    Register(1220, "ID_WAVE_CAPNO2", (p, n, c) => new IdWaveCapno2(p, n));
    Register(1221, "ID_DATA_CAPNO2", (p, n, c) => new IdDataCapno2(p, n));
    Register(1223, "ID_VOLDATA_CAPNO2", (p, n, c) => new IdVolDataCapno2(p, n));
    Register(1224, "ID_WAVE_VCO2_CAPNO2", (p, n, c) => new IdWaveVco2Capno2(p, n));
    Register(1249, "ID_STATUS_SPO", (p, n, c) => new IdStatusSpo(p, n));
    Register(1253, "ID_GET_OPT_SPO", (p, n, c) => new IdGetOptSpo(p, n));
    Register(1284, "ID_VENTILATOR_SN_CIVL2", (p, n, c) => new IdVentilatorSnCivl2(p, n));
    Register(1293, "ID_VEXP_CIVL2", (p, n, c) => new IdVexpCivl2(p, n));
    Register(1294, "ID_PAR5_CIVL2", (p, n, c) => new IdPar5Civl2(p, n));
    Register(1296, "ID_STATUS2_CIVL2", (p, n, c) => new IdStatusCivl2(p, n));
    Register(1298, "ID_PAUX_WAVE_CIVL2", (p, n, c) => new IdPauxWaveCivl2(p, n));
    Register(1299, "ID_PAUX_DATA_CIVL2", (p, n, c) => new IdPauxDataCivl2(p, n));
    Register(1300, "ID_PAUX_DATA2_CIVL2", (p, n, c) => new IdPauxData2Civl2(p, n));
    Register(1304, "ID_NCPAP_CIVL2", (p, n, c) => new IdNcpapCivl2(p, n));
    Register(1345, "ID_OUT_EXTFLOW", (p, n, c) => new IdOutExtflow(p, n));
    Register(1409, "ID_STATUS_SPO_V2_1", (p, n, c) => new IdStatusSpo_v2_1(p, n));
    Register(1410, "ID_STATUS_SPO_V2_2", (p, n, c) => new IdStatusSpo_v2_2(p, n));
    Register(1449, "ID_STATUS_MOTOR", (p, n, c) => new IdStatusMotor(p, n));
    Register(1451, "ID_REAL_SPEED_MOTOR", (p, n, c) => new IdRealSpeedMotor(p, n));
}
}
