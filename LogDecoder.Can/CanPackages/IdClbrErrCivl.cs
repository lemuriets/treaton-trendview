using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

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

    private static readonly Dictionary<int, (string, PackageTechStatus)> BitsDefinitions = new()
    {
        { 0, ("начало калибровки", PackageTechStatus.Info) },
        { 1, ("успешное завершение калибровки", PackageTechStatus.Ok) },
        { 2, ("успешное завершение калибровки, рекомендуется замена датчика кислорода", PackageTechStatus.Warning) },
        { 3, ("датчик кислорода отсутствует или неисправен", PackageTechStatus.Error) },
        { 4, ("нет информации об атмосферном давлении или информация некорректна", PackageTechStatus.Error) },
        { 5, ("отмена калибровки", PackageTechStatus.Warning) },
        { 6, ("выдача напряжения с датчика кислорода", PackageTechStatus.Info) },
        { 7, ("неисправен канал измерения кислорода в КИВЛ", PackageTechStatus.Error) },
        { 8, ("ошибка записи во flash", PackageTechStatus.Error) },
        { 9, ("выдача информации с процентом завершения калибровки", PackageTechStatus.Info) },
        { 10, ("напряжение на датчике выше ранее запомненной величины", PackageTechStatus.Warning) },
        { 11, ("вышел срок жизни датчика", PackageTechStatus.Warning) },
        { 12, ("слишком низкое напряжение с нового датчика кислорода", PackageTechStatus.Warning) },
        { 13, ("слишком высокое напряжение с нового датчика", PackageTechStatus.Warning) },
        { 14, ("ошибка считывания напряжения из файловой системы", PackageTechStatus.Error) },
    };
    private static readonly Dictionary<int, (string, PackageTechStatus)> ValveBitsDefinitions = new()
    {
        { 0, ("начало калибровки", PackageTechStatus.Info) },
        { 1, ("калибровка ЭМКВ успешно завершена", PackageTechStatus.Ok) },
        { 2, ("не удаётся достичь давления 10 см вод.ст.", PackageTechStatus.Error) },
        { 3, ("не удаётся достичь давления 30 см вод.ст.", PackageTechStatus.Error) },
        { 4, ("не удаётся достичь давления 50 см вод.ст.", PackageTechStatus.Error) },
        { 5, ("отсутствует поток на выдохе", PackageTechStatus.Error) },
        { 6, ("не удаётся достичь нулевого потока на выдохе при останове ГП", PackageTechStatus.Error) },
        { 7, ("при потоке 10 л/мин и снятом напряжении с ЭМКВ давление на выдохе превышает 2 см вод.ст.", PackageTechStatus.Warning) },
        { 8, ("большая нелинейность ЭМКВ", PackageTechStatus.Warning) },
        { 9, ("ошибка записи в FLASH", PackageTechStatus.Error) },
        { 10, ("отмена калибровки", PackageTechStatus.Warning) },
        { 11, ("ошибка не откалиброван датчик потока в ЭМКВ", PackageTechStatus.Error) },
        { 12, ("ошибка не откалиброван ДД в маг. выдоха", PackageTechStatus.Error) },
        { 13, ("ошибка не откалиброван дифф. ДД для ДП в ЭМКВ", PackageTechStatus.Error) },
        { 14, ("ошибка калибровочная кривая на потоке 20л/мин не соответствует реальной", PackageTechStatus.Error) },
    };
    private static readonly Dictionary<int, (string, PackageTechStatus)> FlowExhaleBitsDefinitions = new()
    {
        { 0, ("начало калибровки", PackageTechStatus.Info) },
        { 1, ("калибровка успешно завершена", PackageTechStatus.Ok) },
        { 2, ("обновить процент завершения калибровки", PackageTechStatus.Info) },
        { 3, ("отказ смесителя газов", PackageTechStatus.Error) },
        { 4, ("нет ответа от смесителя газов", PackageTechStatus.Error) },
        { 5, ("поток не равен нулю", PackageTechStatus.Warning) },
        { 6, ("неверная скорость передачи данных от смесителя газов", PackageTechStatus.Error) },
        { 7, ("большое давление в магистрали вдоха", PackageTechStatus.Warning) },
        { 8, ("ошибка вычисления аппроксимирующих коэффициентов", PackageTechStatus.Error) },
        { 9, ("ошибочные коэффициенты для датчика давления в магистрали вдоха", PackageTechStatus.Error) },
        { 10, ("ошибочные коэффициенты для дифференциального датчика давления", PackageTechStatus.Error) },
        { 11, ("нет ответа от источника питания", PackageTechStatus.Error) },
        { 12, ("датчик температуры в источнике питания неисправен", PackageTechStatus.Error) },
        { 13, ("низкая температура окружающего воздуха", PackageTechStatus.Warning) },
        { 14, ("остановка задачи калибровки датчика потока в магистрали выдоха", PackageTechStatus.Warning) },
        { 15, ("ошибка монотонности во входной таблице", PackageTechStatus.Error) },
        { 16, ("ошибка записи коэффициентов", PackageTechStatus.Error) },
        { 17, ("ошибочный коэффициент для датчика давления в магистрали выдоха", PackageTechStatus.Error) },
        { 18, ("указывает на содержание в байте 1 максимального значения погрешности", PackageTechStatus.Warning) },
    };
    private static readonly Dictionary<int, (string, PackageTechStatus)> KvtBitsDefinitions = new()
    {
        { 0, ("начало калибровки", PackageTechStatus.Info) },
        { 1, ("ошибка \"Датчик давления на вдохе не откалиброван\"", PackageTechStatus.Error) },
        { 2, ("ошибка \"Датчик давления на выдохе не откалиброван\"", PackageTechStatus.Error) },
        { 3, ("ошибка \"Нет ответа от смесителя газов\"", PackageTechStatus.Error) },
        { 4, ("ошибка \"Неверная частота выдачи пакетов от смесителя газов\"", PackageTechStatus.Error) },
        { 5, ("ошибка \"Низкая температура окружающего воздуха\"", PackageTechStatus.Warning) },
        { 6, ("ошибка \"Датчик температуры в источнике питания неисправен\"", PackageTechStatus.Error) },
        { 7, ("ошибка \"Отмена калибровки\"", PackageTechStatus.Warning) },
        { 8, ("ошибка \"Неисправность смесителя газов\"", PackageTechStatus.Error) },
        { 9, ("ошибка \"Высокое давление в магистрали вдоха\"", PackageTechStatus.Warning) },
        { 10, ("действовать далее", PackageTechStatus.Info) },
        { 11, ("ошибка \"Ошибка монотонности во входных данных\"", PackageTechStatus.Error) },
        { 12, ("ошибка \"Ошибка записи коэффициентов\"", PackageTechStatus.Error) },
        { 13, ("ошибка \"Нет ответа от источника питания\"", PackageTechStatus.Error) },
        { 14, ("действовать далее", PackageTechStatus.Info) },
        { 15, ("обновить процент завершения калибровки", PackageTechStatus.Info) },
        { 16, ("ошибка \"Большой комплайнс контура\"", PackageTechStatus.Warning) },
        { 17, ("ошибка \"Низкий комплайнс контура\"", PackageTechStatus.Warning) },
        { 18, ("ошибка \"Большая утечка из контура\"", PackageTechStatus.Warning) },
        { 19, ("ошибка \"Клапан выдоха не закрывается\"", PackageTechStatus.Error) },
    };
    private static readonly Dictionary<int, (string, PackageTechStatus)> NcPAPBitsDefinitions = new()
    {
        { 0, ("начало калибровки", PackageTechStatus.Info) },
        { 1, ("успешное завершение калибровки", PackageTechStatus.Ok) },
        { 2, ("Error: датчик Paux не подсоединён к тройнику", PackageTechStatus.Error) },
        { 3, ("Error: High inspiratory pressure", PackageTechStatus.Error) },
        { 4, ("Error: Paux pressure sensor did not calibrate", PackageTechStatus.Error) },
        { 5, ("Error: inspiratory pressure sensor did not calibrate", PackageTechStatus.Error) },
        { 6, ("Error: Initial Paux pressure > 1 см", PackageTechStatus.Warning) },
        { 7, ("отмена калибровки", PackageTechStatus.Warning) },
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
            2 => ParseBits(span[0], KvtBitsDefinitions),
            4 => ParseBits(span[0], ValveBitsDefinitions),
            5 => ParseBits(span[0], BitsDefinitions),
            14 => ParseBits(span[0], FlowExhaleBitsDefinitions),
            25 => ParseBits(span[0], NcPAPBitsDefinitions),
            _ => []
        };
        var numericData = new List<NumericDataItem>();

        // согласно документации
        if (_mode == 5 && responseCode == 1)
        {
            numericData.Add(new("Resource", value));
        }
        if (_mode == 5 && responseCode == 6)
        {
            numericData.Add(new("Voltage", value * 0.1));
        }
        if (responseCode == 2 || responseCode == 9 || responseCode == 15)
        {
            numericData.Add(new("Progress", value));
        }
        if (_mode == 14 && responseCode == 18)
        {
            numericData.Add(new("MaxError", value));
        }
        return new PackageData(numericData.ToArray(), messages);
    }
}