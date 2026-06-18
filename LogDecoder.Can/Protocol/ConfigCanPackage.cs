using LogDecoder.CAN.General;
using LogDecoder.CAN.Packages;
using LogDecoder.CAN.Protocol.Definitions;
using LogDecoder.Parser;

namespace LogDecoder.CAN.Protocol;

/// <summary>
/// Generic, YAML-definition-driven package parser. One instance interprets a
/// <see cref="PackageDefinition"/> over a CAN frame, producing the same
/// <see cref="PackageData"/> shape the hand-written package classes did.
/// </summary>
public sealed class ConfigCanPackage : BasePackageParsed
{
    private readonly PackageDefinition _definition;
    private readonly Endianness _defaultEndianness;
    private readonly byte? _mode;

    public ConfigCanPackage(
        CanPackage package,
        string name,
        PackageDefinition definition,
        Endianness defaultEndianness,
        ParseContext context)
        : base(package, name)
    {
        _definition = definition;
        _defaultEndianness = defaultEndianness;
        _mode = definition.RequiresContext is null ? null : context.CivlMode;
    }

    public override PackageData? ParseData()
    {
        if (Data.Length < _definition.Length)
        {
            return null;
        }

        var span = Data.Span;
        var code = span.Length > 0 ? span[0] : -1;

        var numeric = new List<NumericDataItem>();
        var messages = new List<string>();

        if (_definition.Messages is { } constantMessages)
        {
            foreach (var message in constantMessages)
            {
                messages.Add(message.Message ?? string.Empty);
                Escalate(message.Status);
            }
        }

        foreach (var field in _definition.Fields)
        {
            if (field.Gate is { } gate && (gate.Byte >= span.Length || span[gate.Byte] != gate.Value))
            {
                continue;
            }

            switch (field.Kind)
            {
                case FieldKind.DateTime:
                    if (!TryEmitDateTime(field, span, messages))
                    {
                        return null;
                    }
                    break;
                case FieldKind.Bit:
                    EmitBit(field, span, messages);
                    break;
                case FieldKind.BitRange:
                    EmitScalar(field, ExtractBitRange(field, span), code, numeric, messages);
                    break;
                case FieldKind.Value:
                    EmitScalar(field, ExtractValue(field, span), code, numeric, messages);
                    break;
                case FieldKind.Raw:
                    break;
            }
        }

        return new PackageData(numeric, messages);
    }

    private void EmitScalar(FieldDefinition field, long raw, int code, List<NumericDataItem> numeric, List<string> messages)
    {
        if (field.Sentinel is { } sentinel && raw == sentinel.Value)
        {
            if (!string.IsNullOrEmpty(sentinel.Message))
            {
                messages.Add(sentinel.Message);
                Escalate(sentinel.Status);
            }
            return;
        }

        raw = Clamp(field, raw);

        if (field.Cases is { } cases)
        {
            foreach (var fieldCase in cases)
            {
                if (Matches(fieldCase.When, code))
                {
                    LookupMessage(fieldCase.Values, raw, messages);
                    break;
                }
            }
            return;
        }

        if (field.EmitNumeric is { } emits)
        {
            foreach (var emit in emits)
            {
                if (Matches(emit.When, code))
                {
                    numeric.Add(new NumericDataItem(emit.Name, raw * emit.Scale));
                    break;
                }
            }
            return;
        }

        if (field.Values is { } values)
        {
            LookupMessage(values, raw, messages);
            return;
        }

        numeric.Add(new NumericDataItem(field.Name, raw * field.Scale + field.Offset));
    }

    private void EmitBit(FieldDefinition field, ReadOnlySpan<byte> span, List<string> messages)
    {
        if (field.Byte is not { } byteIndex || field.Bit is not { } bit || field.Values is not { } values)
        {
            return;
        }
        var state = BitUtil.IsBitSet(span[byteIndex], bit) ? 1 : 0;
        LookupMessage(values, state, messages);
    }

    private bool TryEmitDateTime(FieldDefinition field, ReadOnlySpan<byte> span, List<string> messages)
    {
        if (field.Bytes is not { Count: 6 } bytes)
        {
            return true;
        }
        try
        {
            var dt = new DateTime(
                field.YearOffset + span[bytes[0]],
                span[bytes[1]],
                span[bytes[2]],
                span[bytes[3]],
                span[bytes[4]],
                span[bytes[5]]);
            messages.Add(dt.ToString(CanConfig.TimeFormat));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private void LookupMessage(Dictionary<int, ValueMessageDefinition> values, long key, List<string> messages)
    {
        if (key < int.MinValue || key > int.MaxValue || !values.TryGetValue((int)key, out var def))
        {
            return;
        }
        messages.Add(def.Message ?? string.Empty);
        Escalate(def.Status);
    }

    private bool Matches(WhenClause? when, int code)
    {
        if (when is null)
        {
            return true;
        }
        if (when.Mode is { } mode && (_mode is null || _mode.Value != mode))
        {
            return false;
        }
        if (when.Code is { } expected && code != expected)
        {
            return false;
        }
        return true;
    }

    private long ExtractValue(FieldDefinition field, ReadOnlySpan<byte> span)
    {
        var endianness = field.Endianness ?? _defaultEndianness;

        if (field.Bytes is { Count: > 0 } bytes)
        {
            ulong raw = 0;
            for (var i = 0; i < bytes.Count; i++)
            {
                var shift = endianness == Endianness.Little ? 8 * i : 8 * (bytes.Count - 1 - i);
                raw |= (ulong)span[bytes[i]] << shift;
            }
            return field.Signed ? SignExtend(raw, 8 * bytes.Count) : (long)raw;
        }

        if (field.Byte is { } single)
        {
            ulong raw = span[single];
            return field.Signed ? SignExtend(raw, 8) : (long)raw;
        }

        return 0;
    }

    private static long ExtractBitRange(FieldDefinition field, ReadOnlySpan<byte> span)
    {
        if (field.Byte is not { } byteIndex || field.BitRange is not { } range)
        {
            return 0;
        }
        return BitUtil.ExtractBits(span[byteIndex], range.From, range.To);
    }

    private static long Clamp(FieldDefinition field, long raw)
    {
        if (field.Max is { } max && raw > max)
        {
            raw = max;
        }
        if (field.Min is { } min && raw < min)
        {
            raw = min;
        }
        return raw;
    }

    private static long SignExtend(ulong value, int bits)
    {
        if (bits >= 64)
        {
            return unchecked((long)value);
        }
        var signBit = 1UL << (bits - 1);
        if ((value & signBit) != 0)
        {
            return (long)value - (long)(1UL << bits);
        }
        return (long)value;
    }

    private void Escalate(PackageTechStatus status)
    {
        TechStatus = (PackageTechStatus)Math.Max((int)TechStatus, (int)status);
    }
}
