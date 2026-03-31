using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

public enum CivlMode
{
    Unknown = -1,
    Waiting = 0,
    Work = 1,
    ShortInternalTest = 2,
    FullInternalTest = 3,
    ExhaleValveCalibration = 4,
    O2SensorCalibration = 5,
    FastOxyCalibration = 6,
    PressureSensorsCalibration = 7,
    EmkvCheck = 8,
    O2FlowSensorCalibration = 9,
    FlowGeneratorCalibration = 10,
    FlowGeneration = 11,
    LeakCheck = 12,
    TechChildMode = 13,
    AirFlowSensorCalibration = 14,
    FlowPressureCalibration = 15,
    EmergencyMode1 = 16,
    EmergencyMode2 = 17,
    EmergencyMode3 = 18,
    FullEmergencyMode = 19,
    ReplaceO2Sensor = 21,
    BootloaderStart = 22,
    Initialization = 23,
    PressureSensorsZeroCalibration = 24,
    NcPapCircuitTest = 25
}

[CanPackageAttr(0x4A0, "Режим КИВЛ")]
public class IdModeCivl : BasePackageParsed
{
    public IdModeCivl(CanPackage p, string name) : base(p, name)
    {
        Mode = GetMode();
    }

    public const int Id = 0x4A0;

    private static readonly Dictionary<int, (string, PackageTechStatus)> BitsDefinitions = new()
    {
        { 0, ("ОЖИДАНИЕ", PackageTechStatus.Info) },
        { 1, ("РАБОТА", PackageTechStatus.Info) },
        { 2, ("Короткий Внутренний Тест", PackageTechStatus.Info) },
        { 3, ("Полный Внутренний Тест", PackageTechStatus.Info) },
        { 4, ("калибровка клапана выдоха", PackageTechStatus.Info) },
        { 5, ("калибровка датчика кислорода на вдохе", PackageTechStatus.Info) },
        { 6, ("калибровка FASTOXY в капнографе бокового отведения", PackageTechStatus.Info) },
        { 7, ("калибровка датчиков давления", PackageTechStatus.Info) },
        { 8, ("проверка ЭМКВ", PackageTechStatus.Info) },
        { 9, ("калибровка датчика потока кислорода в СГ", PackageTechStatus.Info) },
        { 10, ("калибровка генератора потока", PackageTechStatus.Info) },
        { 11, ("генерация потока", PackageTechStatus.Info) },
        { 12, ("проверка утечки", PackageTechStatus.Info) },
        { 13, ("технологический детский режим", PackageTechStatus.Info) },
        { 14, ("калибровка датчика потока воздуха в СГ", PackageTechStatus.Info) },
        { 15, ("калибровка преобразователя поток-давление", PackageTechStatus.Info) },
        { 16, ("АВАРИЙНЫЙ РЕЖИМ №1", PackageTechStatus.Warning) },
        { 17, ("АВАРИЙНЫЙ РЕЖИМ №2", PackageTechStatus.Warning) },
        { 18, ("АВАРИЙНЫЙ РЕЖИМ №3", PackageTechStatus.Warning) },
        { 19, ("ПОЛНОСТЬЮ АВАРИЙНЫЙ РЕЖИМ", PackageTechStatus.Warning) },
        { 21, ("замена датчика кислорода на вдохе", PackageTechStatus.Info) },
        { 22, ("запуск загрузчика для обновления прошивки КИВЛ", PackageTechStatus.Info) },
        { 23, ("Инициализация", PackageTechStatus.Info) },
        { 24, ("калибровка нулей датчиков давления в зависимости от температуры", PackageTechStatus.Info) },
        { 25, ("тест контура для режима nCPAP", PackageTechStatus.Info) },
    };
    
    public CivlMode Mode { get; }

    private CivlMode GetMode()
    {
        if (Data.Length < 1)
        {
            return CivlMode.Unknown;
        }

        return Data.Span[0] switch
        {
            0 => CivlMode.Waiting,
            1 => CivlMode.Work,
            2 => CivlMode.ShortInternalTest,
            3 => CivlMode.FullInternalTest,
            4 => CivlMode.ExhaleValveCalibration,
            5 => CivlMode.O2SensorCalibration,
            6 => CivlMode.FastOxyCalibration,
            7 => CivlMode.PressureSensorsCalibration,
            8 => CivlMode.EmkvCheck,
            9 => CivlMode.O2FlowSensorCalibration,
            10 => CivlMode.FlowGeneratorCalibration,
            11 => CivlMode.FlowGeneration,
            12 => CivlMode.LeakCheck,
            13 => CivlMode.TechChildMode,
            14 => CivlMode.AirFlowSensorCalibration,
            15 => CivlMode.FlowPressureCalibration,
            16 => CivlMode.EmergencyMode1,
            17 => CivlMode.EmergencyMode2,
            18 => CivlMode.EmergencyMode3,
            19 => CivlMode.FullEmergencyMode,
            21 => CivlMode.ReplaceO2Sensor,
            22 => CivlMode.BootloaderStart,
            23 => CivlMode.Initialization,
            24 => CivlMode.PressureSensorsZeroCalibration,
            25 => CivlMode.NcPapCircuitTest,
            _ => CivlMode.Unknown
        };
    }

    public override PackageData? ParseData()
    {
        if (Data.Length < 1)
        {
            return null;
        }
        var messages = ParseBits(Data.Span[0], BitsDefinitions);
        var numericData = Array.Empty<NumericDataItem>();

        return new PackageData(numericData, messages.ToArray());
    }
}