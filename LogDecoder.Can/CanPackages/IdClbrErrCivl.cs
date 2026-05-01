using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4A9, "Ошибки калибровки КИВЛ")]
public class IdClbrErrCivl : BasePackageParsed
{
    public IdClbrErrCivl(CanPackage p, string name, byte? mode = null) : base(p, name)
    {
        _mode = mode;
    }

    private readonly byte? _mode;

    public const int Id = 0x4A9;

    private static readonly Dictionary<ulong, MessageDefinition> OxygenSensorCalibrationDefinitions = new()
    {
        [0] = new("начало калибровки"),
        [1] = new("успешное завершение калибровки", PackageTechStatus.Ok),
        [2] = new("успешное завершение калибровки, рекомендуется замена датчика кислорода", PackageTechStatus.Warning),
        [3] = new("датчик кислорода отсутствует или неисправен", PackageTechStatus.Error),
        [4] = new("нет информации об атмосферном давлении или информация некорректна", PackageTechStatus.Error),
        [5] = new("отмена калибровки", PackageTechStatus.Warning),
        [6] = new("выдача напряжения с датчика кислорода"),
        [7] = new("неисправен канал измерения кислорода в КИВЛ", PackageTechStatus.Error),
        [8] = new("ошибка записи во flash", PackageTechStatus.Error),
        [9] = new("выдача информации с процентом завершения калибровки"),
        [10] = new("напряжение на датчике выше ранее запомненной величины", PackageTechStatus.Warning),
        [11] = new("вышел срок жизни датчика", PackageTechStatus.Warning),
        [12] = new("слишком низкое напряжение с нового датчика кислорода", PackageTechStatus.Warning),
        [13] = new("слишком высокое напряжение с нового датчика", PackageTechStatus.Warning),
        [14] = new("ошибка считывания напряжения из файловой системы", PackageTechStatus.Error),
    };

    private static readonly Dictionary<ulong, MessageDefinition> ValveCalibrationDefinitions = new()
    {
        [0] = new("начало калибровки"),
        [1] = new("калибровка ЭМКВ успешно завершена", PackageTechStatus.Ok),
        [2] = new("не удаётся достичь давления 10 см вод.ст.", PackageTechStatus.Error),
        [3] = new("не удаётся достичь давления 30 см вод.ст.", PackageTechStatus.Error),
        [4] = new("не удаётся достичь давления 50 см вод.ст.", PackageTechStatus.Error),
        [5] = new("отсутствует поток на выдохе", PackageTechStatus.Error),
        [6] = new("не удаётся достичь нулевого потока на выдохе при останове ГП", PackageTechStatus.Error),
        [7] = new("при потоке 10 л/мин и снятом напряжении с ЭМКВ давление на выдохе превышает 2 см вод.ст.", PackageTechStatus.Warning),
        [8] = new("большая нелинейность ЭМКВ", PackageTechStatus.Warning),
        [9] = new("ошибка записи в FLASH", PackageTechStatus.Error),
        [10] = new("отмена калибровки", PackageTechStatus.Warning),
        [11] = new("ошибка не откалиброван датчик потока в ЭМКВ", PackageTechStatus.Error),
        [12] = new("ошибка не откалиброван ДД в маг. выдоха", PackageTechStatus.Error),
        [13] = new("ошибка не откалиброван дифф. ДД для ДП в ЭМКВ", PackageTechStatus.Error),
        [14] = new("ошибка калибровочная кривая на потоке 20л/мин не соответствует реальной", PackageTechStatus.Error),
    };

    private static readonly Dictionary<ulong, MessageDefinition> FlowExhaleCalibrationDefinitions = new()
    {
        [0] = new("начало калибровки"),
        [1] = new("калибровка успешно завершена", PackageTechStatus.Ok),
        [2] = new("обновить процент завершения калибровки"),
        [3] = new("отказ смесителя газов", PackageTechStatus.Error),
        [4] = new("нет ответа от смесителя газов", PackageTechStatus.Error),
        [5] = new("поток не равен нулю", PackageTechStatus.Warning),
        [6] = new("неверная скорость передачи данных от смесителя газов", PackageTechStatus.Error),
        [7] = new("большое давление в магистрали вдоха", PackageTechStatus.Warning),
        [8] = new("ошибка вычисления аппроксимирующих коэффициентов", PackageTechStatus.Error),
        [9] = new("ошибочные коэффициенты для датчика давления в магистрали вдоха", PackageTechStatus.Error),
        [10] = new("ошибочные коэффициенты для дифференциального датчика давления", PackageTechStatus.Error),
        [11] = new("нет ответа от источника питания", PackageTechStatus.Error),
        [12] = new("датчик температуры в источнике питания неисправен", PackageTechStatus.Error),
        [13] = new("низкая температура окружающего воздуха", PackageTechStatus.Warning),
        [14] = new("остановка задачи калибровки датчика потока в магистрали выдоха", PackageTechStatus.Warning),
        [15] = new("ошибка монотонности во входной таблице", PackageTechStatus.Error),
        [16] = new("ошибка записи коэффициентов", PackageTechStatus.Error),
        [17] = new("ошибочный коэффициент для датчика давления в магистрали выдоха", PackageTechStatus.Error),
        [18] = new("указывает на содержание в байте 1 максимального значения погрешности", PackageTechStatus.Warning),
    };

    private static readonly Dictionary<ulong, MessageDefinition> KvtCalibrationDefinitions = new()
    {
        [0] = new("начало калибровки"),
        [1] = new("ошибка \"Датчик давления на вдохе не откалиброван\"", PackageTechStatus.Error),
        [2] = new("ошибка \"Датчик давления на выдохе не откалиброван\"", PackageTechStatus.Error),
        [3] = new("ошибка \"Нет ответа от смесителя газов\"", PackageTechStatus.Error),
        [4] = new("ошибка \"Неверная частота выдачи пакетов от смесителя газов\"", PackageTechStatus.Error),
        [5] = new("ошибка \"Низкая температура окружающего воздуха\"", PackageTechStatus.Warning),
        [6] = new("ошибка \"Датчик температуры в источнике питания неисправен\"", PackageTechStatus.Error),
        [7] = new("ошибка \"Отмена калибровки\"", PackageTechStatus.Warning),
        [8] = new("ошибка \"Неисправность смесителя газов\"", PackageTechStatus.Error),
        [9] = new("ошибка \"Высокое давление в магистрали вдоха\"", PackageTechStatus.Warning),
        [10] = new("действовать далее"),
        [11] = new("ошибка \"Ошибка монотонности во входных данных\"", PackageTechStatus.Error),
        [12] = new("ошибка \"Ошибка записи коэффициентов\"", PackageTechStatus.Error),
        [13] = new("ошибка \"Нет ответа от источника питания\"", PackageTechStatus.Error),
        [14] = new("действовать далее"),
        [15] = new("обновить процент завершения калибровки"),
        [16] = new("ошибка \"Большой комплайнс контура\"", PackageTechStatus.Warning),
        [17] = new("ошибка \"Низкий комплайнс контура\"", PackageTechStatus.Warning),
        [18] = new("ошибка \"Большая утечка из контура\"", PackageTechStatus.Warning),
        [19] = new("ошибка \"Клапан выдоха не закрывается\"", PackageTechStatus.Error),

    };

    private static readonly Dictionary<ulong, MessageDefinition> NcPapCalibrationDefinitions = new()

    {
        [0] = new("начало калибровки"),
        [1] = new("успешное завершение калибровки", PackageTechStatus.Ok),
        [2] = new("Error: датчик Paux не подсоединён к тройнику", PackageTechStatus.Error),
        [3] = new("Error: High inspiratory pressure", PackageTechStatus.Error),
        [4] = new("Error: Paux pressure sensor did not calibrate", PackageTechStatus.Error),
        [5] = new("Error: inspiratory pressure sensor did not calibrate", PackageTechStatus.Error),
        [6] = new("Error: Initial Paux pressure > 1 см", PackageTechStatus.Warning),
        [7] = new("отмена калибровки", PackageTechStatus.Warning),

    };

    public override PackageData? ParseData()
    {
        if (Data.Length < 1)
        {
            return null;
        }
        var span = Data.Span;

        var responseCode = span[0];
        var value = Data.Length > 1 ? span[1] : 0;
        
        var messages = _mode switch
        {
            2 => ParseValueMessageAndUpdateStatus(span[0], OxygenSensorCalibrationDefinitions),
            4 => ParseValueMessageAndUpdateStatus(span[0], ValveCalibrationDefinitions),
            5 => ParseValueMessageAndUpdateStatus(span[0], FlowExhaleCalibrationDefinitions),
            14 => ParseValueMessageAndUpdateStatus(span[0], KvtCalibrationDefinitions),
            25 => ParseValueMessageAndUpdateStatus(span[0], NcPapCalibrationDefinitions),
            _ => []
        };
        var numericData = new List<NumericDataItem>();

        // согласно документации
        if (_mode == (byte)CivlMode.O2SensorCalibration && responseCode == 1)
        {
            numericData.Add(new("Resource", value));
        }
        else if (_mode == (byte)CivlMode.O2SensorCalibration && responseCode == 6)
        {
            numericData.Add(new("Voltage", value * 0.1));
        }
        else if (_mode == (byte)CivlMode.ShortInternalTest || _mode == (byte)CivlMode.AirFlowSensorCalibration && responseCode == 2)
        {
            numericData.Add(new("Progress", value));
        }
        else if (_mode == (byte)CivlMode.AirFlowSensorCalibration && responseCode == 18)
        {
            numericData.Add(new("MaxError", value));
        }
        return new PackageData(numericData, messages);
    }
}