using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x482, "Состояние метаболографа")]
    public class IdStatusCapno1 : BasePackageParsed
    {
        public const int Id = 0x482;

        private static readonly Dictionary<int, (string msg, PackageTechStatus level)> StatusDefinitions = new()
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

            var span = Data.Span;

            var status = span[0];
            var flags = span[1];

            var numericData = new NumericDataItem[]
            {
                new("status", status),
                new("flags", flags),
            };

            var messages = new List<string>();

            if (StatusDefinitions.TryGetValue(status, out var statusInfo))
            {
                messages.Add(statusInfo.msg);
                TechStatus = statusInfo.level;
            }
            else
            {
                messages.Add($"Неизвестный код состояния: 0x{status:X2}");
                TechStatus = PackageTechStatus.Warning;
            }

            messages.Add(((flags & (1 << 0)) == 0)
                ? "Режим отображения: в мм рт.ст."
                : "Режим отображения: в процентах");

            messages.Add(((flags & (1 << 1)) == 0)
                ? "Деморежим: выкл."
                : "Деморежим: вкл.");

            messages.Add(((flags & (1 << 2)) == 0)
                ? "Режим синхронизации: внутренняя"
                : "Режим синхронизации: внешняя");

            if ((flags & (1 << 3)) != 0)
            {
                messages.Add("Подключена линия отбора пробы");
            }

            if ((flags & (1 << 4)) != 0)
            {
                messages.Add("Наличие в приборе блока измерения N2O");
            }

            if ((flags & (1 << 5)) != 0)
            {
                messages.Add("Наличие в приборе блока измерения O2");
            }

            messages.Add(((flags & (1 << 6)) == 0)
                ? "Режим измерений: обычные циклические измерения"
                : "Режим измерений: измерения в дыхательной паузе");

            messages.Add(((flags & (1 << 7)) == 0)
                ? "Режим работы: капнограф"
                : "Режим работы: метаболограф");

            return new PackageData(numericData, messages);
        }
    }