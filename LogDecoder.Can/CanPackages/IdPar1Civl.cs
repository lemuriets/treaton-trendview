using LogDecoder.CAN.Attributes;
using LogDecoder.CAN.CanPackages;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;

namespace LogDecoder.CAN.Packages;

[CanPackageAttr(0x4A3, "Параметры вентиляции 1")]
public class IdPar1Civl : BasePackageParsed
{
    public IdPar1Civl(CanPackage p, string name) : base(p, name) { }

    public const int Id = 0x4A3;

    // биты байта 0 (кроме режима)
    private static readonly Dictionary<int, BitMessage> BitDefinitions = new()
    {
        { 7, BitMessage.Both("Взрослый режим", "Детский режим") },
        { 6, BitMessage.Both("Нет режима вздох", "Режим вздох") },
        { 5, BitMessage.Both("Поток прямоугольный", "Поток убывающий") },
        { 4, BitMessage.Both("Активен триггер по давлению", "Активен триггер по потоку") },
    };

    // режимы (биты 3..0)
    private static readonly Dictionary<int, string> Modes = new()
    {
        {0, "CMV/VCV"},
        {1, "CMV/PCV"},
        {2, "SIMV/PC+PS+apnea"},
        {3, "SIMV/VC+PS+apnea"},
        {4, "CPAP+PS+apnea"},
        {5, "BiSTEP+PS+apnea"},
        {6, "NIV"},
        {7, "APRV"},
        {8, "PCV-VG"},
        {9, "SIMV/DC"},
        {10, "iSV"},
        {11, "nCPAP"},
        {12, "CPAP+VS"},
        {13, "nIMV"},
        {14, "HF_O2"},
        {15, "Зарезервировано"},
    };

    public override PackageData? ParseData()
    {
        if (Data.Length < 8)
        {
            return null;
        }

        var span = Data.Span;

        var messages = ParseBitsAndUpdateStatus(span[0], BitDefinitions);

        var mode = span[0] & 0x0F;
        if (Modes.TryGetValue(mode, out var modeInfo))
        {
            messages.Add($"Mode: {modeInfo}");
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
        return new PackageData(numericData, messages);
    }
}