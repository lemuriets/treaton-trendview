using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x482, "Состояние метаболографа")]
    public class IdStatusCapno1 : BasePackageParsed
    {
        public const int Id = 0x482;

        // TODO: добавить кодов и доработать парсинг
        private static readonly Dictionary<int, (string msg, PackageTechStatus level)> BitsDefinitions = new()
        {
            { 0x00, ("Измерение", PackageTechStatus.Ok) },
            { 0x01, ("Калибровка нуля", PackageTechStatus.Info) },
            { 0x02, ("Прогрев", PackageTechStatus.Info) },
            { 0x03, ("Закупорка", PackageTechStatus.Warning) },
            { 0x04, ("Просушка пневмотракта", PackageTechStatus.Info) },
            { 0x05, ("Демонстрация", PackageTechStatus.Info) },
            { 0x06, ("Не подключена линия", PackageTechStatus.Warning) },
            { 0x10, ("Отключён пользователем", PackageTechStatus.Info) },
            { 0x11, ("Откл., закупорка", PackageTechStatus.Warning) },
            { 0x12, ("Откл., неисправность излучателя", PackageTechStatus.Error) },
            { 0x13, ("Откл., неисправность приемника", PackageTechStatus.Error) },
            { 0x14, ("Откл., неисправность компрессора", PackageTechStatus.Error) },
            { 0x15, ("Откл., неисправность датчика давления", PackageTechStatus.Error) },
            { 0x16, ("Откл., перегрев", PackageTechStatus.Error) },
            { 0x17, ("Откл., капнограф не использовался >1h", PackageTechStatus.Info) },
            { 0x18, ("Откл., залита камера", PackageTechStatus.Error) },
            { 0x19, ("Откл., продувка неудачна", PackageTechStatus.Error) }
        };

        public IdStatusCapno1(CanPackage p, string name) : base(p, name) { }

        public override PackageData? ParseData()
        {
            if (Data.Length < 4)
            {
                return null;
            }

            var status = Data[0];
            var b1 = Data[1];

            var numericData = new NumericDataItem[]
            {
                new("status", status),
                new("b1", b1),
            };
            var messages = ParseBits(status, BitsDefinitions);

            return new PackageData(numericData, messages);
        }
    }