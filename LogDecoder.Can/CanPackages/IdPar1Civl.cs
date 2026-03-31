using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.General;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4A3, "Параметры вентиляции 1")]
public class IdPar1Civl : BasePackageParsed
{
    public IdPar1Civl(CanPackage p, string name) : base(p, name) { }

    public const int Id = 0x4A3;

    // биты байта 0 (кроме режима)
    private static readonly Dictionary<int, (string, PackageTechStatus)> BitDefinitions = new()
    {
        { 7, ("Детский режим", PackageTechStatus.Info) },
        { 6, ("Режим вдох активен", PackageTechStatus.Info) },
        { 5, ("Убывающий поток", PackageTechStatus.Info) },
        { 4, ("Триггер по потоку", PackageTechStatus.Info) },
    };

    // режимы (биты 3..0)
    private static readonly Dictionary<int, (string, PackageTechStatus)> ModeBitDefinitions = new()
    {
        {0, ("CMV/VCV", PackageTechStatus.Ok)},
        {1, ("CMV/PCV", PackageTechStatus.Ok)},
        {2, ("SIMV/PC+PS+apnea", PackageTechStatus.Ok)},
        {3, ("SIMV/VC+PS+apnea", PackageTechStatus.Ok)},
        {4, ("CPAP+PS+apnea", PackageTechStatus.Ok)},
        {5, ("BiSTEP+PS+apnea", PackageTechStatus.Ok)},
        {6, ("NIV", PackageTechStatus.Ok)},
        {7, ("APRV", PackageTechStatus.Ok)},
        {8, ("PCV-VG", PackageTechStatus.Ok)},
        {9, ("SIMV/DC", PackageTechStatus.Ok)},
        {10, ("iSV", PackageTechStatus.Ok)},
        {11, ("nCPAP", PackageTechStatus.Ok)},
        {12, ("CPAP+VS", PackageTechStatus.Ok)},
        {13, ("nIMV", PackageTechStatus.Ok)},
        {14, ("HF_O2", PackageTechStatus.Ok)},
        {15, ("Зарезервировано", PackageTechStatus.Warning)},
    };

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }

        var span = Data.Span;

        var messages = new List<string>();
        messages.AddRange(ParseBits(span[0], BitDefinitions));

        var mode = span[0] & 0x0F;
        if (ModeBitDefinitions.TryGetValue(mode, out var modeInfo))
        {
            messages.Add($"Mode: {modeInfo.Item1}");
        }

        var pdkv = span[1]; // см вод ст
        var triggerPressure = Math.Min(span[2], (byte)200);
        var triggerFlow = span[3] * 0.1;

        var inspTimeRaw = BitUtil.ToU16(span[4], span[5]);
        var expTimeRaw = BitUtil.ToU16(span[6], span[7]);

        var numericData = new List<NumericDataItem>
        {
            new("ПДКВ [см вод.ст.]", pdkv),
            new("Чувствительность триггера по давлению [мм вод. ст.]", triggerPressure),
            new("Чувствительность триггера по потоку [0.1 л/мин]", triggerFlow),
        };

        if (inspTimeRaw != 0xFFFF)
        {
            numericData.Add(new("Время вдоха [мс]", inspTimeRaw));
        }
        else
        {
            messages.Add("Время вдоха: авто-режим");
        }

        if (expTimeRaw != 0xFFFF)
        {
            numericData.Add(new("Время выдоха [мс]", expTimeRaw));
        }
        else
        {
            messages.Add("Время выдоха: авто-режим");
        }
        return new PackageData(numericData.ToArray(), messages.ToArray());
    }
}