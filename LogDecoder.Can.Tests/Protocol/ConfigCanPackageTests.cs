using LogDecoder.CAN;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Protocol;
using LogDecoder.CAN.Protocol.Definitions;
using LogDecoder.Parser;

namespace LogDecoder.Can.Tests.Protocol;

[TestFixture]
public class ConfigCanPackageTests
{
    private static PackageData Parse(PackageDefinition def, byte[] data, byte? mode = null)
    {
        var context = new ParseContext { CivlMode = mode };
        var package = new CanPackage(PackageType.Standard, def.Id, data, 0, 0);
        var parsed = new ConfigCanPackage(package, def.Name, def, Endianness.Little, context);
        var result = parsed.ParseData();
        Assert.That(result, Is.Not.Null, "ParseData returned null unexpectedly");
        return result!.Value;
    }

    private static PackageDefinition Def(int length, params FieldDefinition[] fields) =>
        new() { Id = 1, Name = "TEST", Length = length, Fields = fields.ToList() };

    [Test]
    public void ConstantMessages_ZeroDataPackage_AlwaysEmitted()
    {
        var def = new PackageDefinition
        {
            Id = 1023,
            Name = "ID_OFF_PWR",
            Length = 0,
            Messages = [new ValueMessageDefinition { Message = "выключения питания", Status = PackageTechStatus.Ok }]
        };
        var data = Parse(def, Array.Empty<byte>());
        Assert.That(data.Messages, Is.EqualTo(new[] { "выключения питания" }));
    }

    [Test]
    public void Sentinel_Match_EmitsMessageNotNumeric()
    {
        var def = Def(2, new FieldDefinition
        {
            Name = "InspTime",
            Kind = FieldKind.Value,
            Bytes = [0, 1],
            Sentinel = new SentinelDefinition { Value = 65535, Message = "авто-режим", Status = PackageTechStatus.Info }
        });

        var matched = Parse(def, new byte[] { 0xFF, 0xFF });
        Assert.That(matched.Messages, Is.EqualTo(new[] { "авто-режим" }));
        Assert.That(matched.NumericData, Is.Empty);

        var normal = Parse(def, new byte[] { 0x10, 0x00 });
        Assert.That(normal.Messages, Is.Empty);
        Assert.That(normal.NumericData.Single().Value, Is.EqualTo(16));
    }

    [Test]
    public void Offset_AppliedAfterScale()
    {
        var def = Def(1, new FieldDefinition { Name = "V", Kind = FieldKind.Value, Byte = 0, Scale = 0.1, Offset = 10 });
        Assert.That(Parse(def, new byte[] { 50 }).NumericData.Single().Value, Is.EqualTo(15).Within(1e-9));
    }

    [Test]
    public void Gate_FieldEmittedOnlyWhenGateMatches()
    {
        var def = Def(2,
            new FieldDefinition
            {
                Name = "Status",
                Kind = FieldKind.Value,
                Byte = 0,
                Gate = new FieldGate { Byte = 1, Value = 0 },
                Values = new() { [3] = new ValueMessageDefinition { Message = "alarm" } }
            });

        Assert.That(Parse(def, new byte[] { 3, 0 }).Messages, Is.EqualTo(new[] { "alarm" }));
        Assert.That(Parse(def, new byte[] { 3, 20 }).Messages, Is.Empty);
    }

    [Test]
    public void Max_ClampsRawValue()
    {
        var def = Def(1, new FieldDefinition { Name = "Trig", Kind = FieldKind.Value, Byte = 0, Max = 200 });
        Assert.That(Parse(def, new byte[] { 250 }).NumericData.Single().Value, Is.EqualTo(200));
        Assert.That(Parse(def, new byte[] { 100 }).NumericData.Single().Value, Is.EqualTo(100));
    }

    [Test]
    public void Value_SingleByte_EmitsNumeric()
    {
        var def = Def(1, new FieldDefinition { Name = "A", Kind = FieldKind.Value, Byte = 0 });
        var data = Parse(def, new byte[] { 42 });
        Assert.That(data.NumericData, Is.EquivalentTo(new[] { new NumericDataItem("A", 42) }));
    }

    [Test]
    public void Value_MultiByteLittleEndian_WithScale()
    {
        var def = Def(2, new FieldDefinition { Name = "A", Kind = FieldKind.Value, Bytes = [0, 1], Scale = 0.1 });
        var data = Parse(def, new byte[] { 0x10, 0x00 });
        Assert.That(data.NumericData.Single().Value, Is.EqualTo(1.6).Within(1e-9));
    }

    [Test]
    public void Value_Signed_TwosComplement()
    {
        var def = Def(2, new FieldDefinition { Name = "A", Kind = FieldKind.Value, Bytes = [0, 1], Signed = true });
        var data = Parse(def, new byte[] { 0x00, 0x80 });
        Assert.That(data.NumericData.Single().Value, Is.EqualTo(-32768));
    }

    [Test]
    public void Bit_Set_EmitsMessage_AndEscalates()
    {
        var def = Def(1, new FieldDefinition
        {
            Name = "B",
            Kind = FieldKind.Bit,
            Byte = 0,
            Bit = 3,
            Values = new() { [1] = new ValueMessageDefinition { Message = "set", Status = PackageTechStatus.Warning } }
        });
        var data = Parse(def, new byte[] { 0b0000_1000 });
        Assert.That(data.Messages, Is.EqualTo(new[] { "set" }));
    }

    [Test]
    public void Bit_Clear_NoMessage()
    {
        var def = Def(1, new FieldDefinition
        {
            Name = "B",
            Kind = FieldKind.Bit,
            Byte = 0,
            Bit = 3,
            Values = new() { [1] = new ValueMessageDefinition { Message = "set" } }
        });
        var data = Parse(def, new byte[] { 0 });
        Assert.That(data.Messages, Is.Empty);
    }

    [Test]
    public void BitRange_Numeric()
    {
        var def = Def(1, new FieldDefinition
        {
            Name = "R",
            Kind = FieldKind.BitRange,
            Byte = 0,
            BitRange = new BitRangeDefinition { From = 0, To = 3 }
        });
        var data = Parse(def, new byte[] { 0x0A });
        Assert.That(data.NumericData.Single().Value, Is.EqualTo(10));
    }

    [Test]
    public void BitRange_MessageLookup()
    {
        var def = Def(1, new FieldDefinition
        {
            Name = "R",
            Kind = FieldKind.BitRange,
            Byte = 0,
            BitRange = new BitRangeDefinition { From = 0, To = 3 },
            Values = new() { [2] = new ValueMessageDefinition { Message = "two" } }
        });
        var data = Parse(def, new byte[] { 0x02 });
        Assert.That(data.Messages, Is.EqualTo(new[] { "two" }));
    }

    [Test]
    public void DateTime_Valid_FormatsTimestamp()
    {
        var def = Def(6, new FieldDefinition
        {
            Name = "T",
            Kind = FieldKind.DateTime,
            Bytes = [0, 1, 2, 3, 4, 5],
            YearOffset = 2000
        });
        var data = Parse(def, new byte[] { 25, 6, 18, 12, 30, 45 });
        Assert.That(data.Messages.Single(), Is.EqualTo("18.06.2025 12:30:45"));
    }

    [Test]
    public void DateTime_Invalid_ReturnsNull()
    {
        var def = Def(6, new FieldDefinition
        {
            Name = "T",
            Kind = FieldKind.DateTime,
            Bytes = [0, 1, 2, 3, 4, 5],
            YearOffset = 2000
        });
        var package = new CanPackage(PackageType.Standard, 1, new byte[] { 25, 13, 18, 12, 30, 45 }, 0, 0);
        var parsed = new ConfigCanPackage(package, "T", def, Endianness.Little, new ParseContext());
        Assert.That(parsed.ParseData(), Is.Null);
    }

    [Test]
    public void ShortPayload_ReturnsNull()
    {
        var def = Def(4, new FieldDefinition { Name = "A", Kind = FieldKind.Value, Byte = 0 });
        var package = new CanPackage(PackageType.Standard, 1, new byte[] { 1, 2 }, 0, 0);
        var parsed = new ConfigCanPackage(package, "A", def, Endianness.Little, new ParseContext());
        Assert.That(parsed.ParseData(), Is.Null);
    }

    [Test]
    public void Cases_SelectedByMode()
    {
        var def = new PackageDefinition
        {
            Id = 1,
            Name = "C",
            Length = 1,
            RequiresContext = "civlMode",
            Fields =
            [
                new FieldDefinition
                {
                    Name = "Code",
                    Kind = FieldKind.Value,
                    Byte = 0,
                    Cases =
                    [
                        new FieldCase
                        {
                            When = new WhenClause { Mode = 5 },
                            Values = new() { [1] = new ValueMessageDefinition { Message = "mode5-one" } }
                        }
                    ]
                }
            ]
        };

        Assert.That(Parse(def, new byte[] { 1 }, mode: 5).Messages, Is.EqualTo(new[] { "mode5-one" }));
        Assert.That(Parse(def, new byte[] { 1 }, mode: 4).Messages, Is.Empty);
    }

    [Test]
    public void EmitNumeric_SelectedByModeAndCode()
    {
        var def = new PackageDefinition
        {
            Id = 1,
            Name = "C",
            Length = 2,
            RequiresContext = "civlMode",
            Fields =
            [
                new FieldDefinition { Name = "Code", Kind = FieldKind.Value, Byte = 0 },
                new FieldDefinition
                {
                    Name = "Val",
                    Kind = FieldKind.Value,
                    Byte = 1,
                    EmitNumeric =
                    [
                        new NumericEmit { When = new WhenClause { Mode = 5, Code = 1 }, Name = "Resource" }
                    ]
                }
            ]
        };

        var data = Parse(def, new byte[] { 1, 100 }, mode: 5);
        Assert.That(data.NumericData, Contains.Item(new NumericDataItem("Resource", 100)));

        var noMatch = Parse(def, new byte[] { 2, 100 }, mode: 5);
        Assert.That(noMatch.NumericData.Any(n => n.Name == "Resource"), Is.False);
    }
}
