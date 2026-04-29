using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x422, "Состояние платы CAN-Ethernet")]
public class IdStatusScm : BasePackageParsed
{
    public const int Id = 0x422;

    private static readonly Dictionary<int, BitMessage> BitsDefinitions = new()
{
    [0] = BitMessage.One(
        "Признак ошибки ИМС физического доступа к шине Ethernet. При её неисправности будет недоступно подключение к шине Ethernet",
        PackageTechStatus.Error),

    [1] = BitMessage.Both(
        "Нет активного подключения к сети Ethernet",
        "Есть активное подключение к сети Ethernet"),

    [2] = BitMessage.Both(
        "Полудуплексный режим обмена Ethernet",
        "Полнодуплексный режим обмена Ethernet"),

    [3] = BitMessage.Both(
        "Скорость обмена по шине Ethernet 10 МБод",
        "Скорость обмена по шине Ethernet 100 МБод"),

    [4] = BitMessage.Both(
        "Нет связи по управляющему порту Ethernet",
        "Есть связь по управляющему порту Ethernet"),

    [5] = BitMessage.Both(
        "Нет связи по порту передачи данных Ethernet",
        "Есть связь по порту передачи данных Ethernet"),

    [6] = BitMessage.Both(
        "SDRAM неисправна. Будет отключена функциональность записи лога CAN на SD карту",
        "SDRAM исправна"),

    [7] = BitMessage.Both(
        "Есть microSD",
        "Нет microSD. Будет отключена функциональность записи лога CAN на SD карту"),

    [8] = BitMessage.Both(
        "Нет процесса создания файловой системы на SD card",
        "Имеется процесс создания файловой системы на SD card"),

    [9] = BitMessage.One(
        "Признак износа SD card. Исчерпан ресурс записи карты памяти SD",
        PackageTechStatus.Warning),

    [10] = BitMessage.One(
        "Признак ошибки обмена с SD card. Блокируется запись лога CAN на SD карту",
        PackageTechStatus.Error),

    [11] = BitMessage.Both(
        "USB mass storage не подключена",
        "USB mass storage подключена"),

    [12] = BitMessage.Both(
        "Нет раздела с FAT на подключенной USB mass storage",
        "Есть раздел с FAT на подключенной USB mass storage"),

    [13] = BitMessage.Both(
        "Нет процесса записи журнала тревог",
        "Имеется процесс записи журнала тревог"),

    [14] = BitMessage.Both(
        "Нет процесса записи копии экрана",
        "Имеется процесс записи копии экрана"),

    [15] = BitMessage.Both(
        "Нет процесса переноса лога CAN с карты памяти SD на носитель USB",
        "Имеется процесс переноса лога CAN с карты памяти SD на носитель USB"),

    [16] = BitMessage.Both(
        "Нет процесса записи журнала сервисного обслуживания аппарата",
        "Имеется процесс записи журнала сервисного обслуживания аппарата"),

    [17] = BitMessage.Both(
        "Нет процесса записи трендов",
        "Имеется процесс записи трендов"),

    [18] = BitMessage.Both(
        "Сервисный режим трансляции выключен (SERVICE_ON)",
        "Сервисный режим трансляции включен (SERVICE_ON)"),

    [19] = BitMessage.One(
        "Ошибка восстановления записываемого сектора microSD из памяти, питающейся от батареи",
        PackageTechStatus.Error),

    [20] = BitMessage.Both(
        "Режим работы с СЦМ выключен (SCM_ON)",
        "Режим работы с СЦМ включен (SCM_ON)"),
};

    public IdStatusScm(CanPackage p, string name) : base(p, name) { }

    public override PackageData? ParseData()
    {
        if (Data.Length < 4)
        {
            return null;
        }
        var span = Data.Span;

        var numericData = Array.Empty<NumericDataItem>();
        var messages = ParseBitsAndUpdateStatus(span[0], BitsDefinitions);

        return new PackageData(numericData, messages);
    }
}

