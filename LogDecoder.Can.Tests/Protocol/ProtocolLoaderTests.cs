using LogDecoder.CAN;
using LogDecoder.CAN.Packages;
using LogDecoder.CAN.Protocol;
using LogDecoder.CAN.Protocol.Definitions;
using LogDecoder.Parser;

namespace LogDecoder.Can.Tests.Protocol;

[TestFixture]
public class ProtocolLoaderTests
{
    private readonly List<string> _tempDirs = new();

    private const string MinimalPackage =
        "id: {0}\nname: PKG{0}\nlength: 1\nfields:\n- name: A\n  kind: Value\n  byte: 0\n";

    [TearDown]
    public void TearDown()
    {
        foreach (var dir in _tempDirs.Where(Directory.Exists))
        {
            Directory.Delete(dir, recursive: true);
        }
        _tempDirs.Clear();
    }

    private string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    private static void WritePackage(string dir, int id)
    {
        File.WriteAllText(Path.Combine(dir, $"{id}.yaml"), string.Format(MinimalPackage, id));
    }

    [Test]
    public void LoadActiveFamily_ShippedConfig_LoadsExpectedPackages()
    {
        var loaded = ProtocolLoader.LoadActiveFamily(TestConfig.Root());

        Assert.That(loaded.Packages.Count, Is.GreaterThanOrEqualTo(49));
        var ids = loaded.Packages.Select(p => p.Id).ToHashSet();
        Assert.That(ids, Is.SupersetOf(new[] { 1120, 1184, 1193, 1409, 1410 }));

        var synchro = loaded.Packages.Single(p => p.Id == 1120);
        Assert.That(synchro.Fields.Single().Kind, Is.EqualTo(FieldKind.DateTime));

        var mode = loaded.Packages.Single(p => p.Id == 1184);
        Assert.That(mode.SetsContext, Is.Not.Null);
        Assert.That(mode.SetsContext!.Key, Is.EqualTo("civlMode"));

        var clbr = loaded.Packages.Single(p => p.Id == 1193);
        Assert.That(clbr.RequiresContext, Is.EqualTo("civlMode"));
    }

    [Test]
    public void ListFamilies_ShippedConfig_ReturnsFamilyWithDisplayName()
    {
        var families = ProtocolLoader.ListFamilies(TestConfig.Root());

        Assert.That(families.Count, Is.GreaterThanOrEqualTo(1));
        var family = families.First(f => f.FolderName == "mv200_300_350");
        Assert.That(family.DisplayName, Is.Not.Empty);
        Assert.That(family.FullPath, Does.Exist);
    }

    [Test]
    public void ListFamilies_SkipsFolderWithoutManifest()
    {
        var root = NewTempDir();
        var good = Path.Combine(root, "good");
        var bad = Path.Combine(root, "bad");
        Directory.CreateDirectory(good);
        Directory.CreateDirectory(bad);
        File.WriteAllText(Path.Combine(good, "manifest.yaml"), "protocol:\n  name: Good\n  synchroId: 1\n");
        WritePackage(good, 1);
        WritePackage(bad, 2);

        var families = ProtocolLoader.ListFamilies(root);

        Assert.That(families.Select(f => f.FolderName), Is.EqualTo(new[] { "good" }));
    }

    [Test]
    public void MissingFolder_Throws()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Assert.Throws<ConfigFolderMissingException>(() => ProtocolLoader.LoadActiveFamily(missing));
    }

    [Test]
    public void EmptyConfig_Throws()
    {
        var root = NewTempDir();
        Assert.Throws<ConfigFolderMissingException>(() => ProtocolLoader.LoadActiveFamily(root));
    }

    [Test]
    public void MultipleFamilies_Throws()
    {
        var root = NewTempDir();
        var familyA = Path.Combine(root, "familyA");
        var familyB = Path.Combine(root, "familyB");
        Directory.CreateDirectory(familyA);
        Directory.CreateDirectory(familyB);
        WritePackage(familyA, 1);
        WritePackage(familyB, 2);

        Assert.Throws<AmbiguousConfigException>(() => ProtocolLoader.LoadActiveFamily(root));
    }

    [Test]
    public void DuplicateId_Throws()
    {
        var family = NewTempDir();
        File.WriteAllText(Path.Combine(family, "10.yaml"), string.Format(MinimalPackage, 1));
        File.WriteAllText(Path.Combine(family, "11.yaml"), string.Format(MinimalPackage, 1));

        Assert.Throws<ConfigValidationException>(() => ProtocolLoader.LoadFamily(family));
    }

    [Test]
    public void PackagesPath_GlobExcludesNonMatchingFiles()
    {
        var family = NewTempDir();
        File.WriteAllText(Path.Combine(family, "manifest.yaml"),
            "protocol:\n  name: T\n  synchroId: 7\npackagesPath: \"[0-9]*.yaml\"\n");
        WritePackage(family, 7);
        File.WriteAllText(Path.Combine(family, "notes.yaml"), string.Format(MinimalPackage, 8));

        var loaded = ProtocolLoader.LoadFamily(family);

        Assert.That(loaded.Packages.Select(p => p.Id), Is.EqualTo(new[] { 7 }));
    }

    [Test]
    public void SynchroIdNotAmongPackages_Throws()
    {
        var family = NewTempDir();
        File.WriteAllText(Path.Combine(family, "manifest.yaml"),
            "protocol:\n  name: T\n  synchroId: 999\npackagesPath: \"[0-9]*.yaml\"\n");
        WritePackage(family, 7);

        Assert.Throws<ConfigValidationException>(() => ProtocolLoader.LoadFamily(family));
    }

    [Test]
    public void ByteOutOfRange_Throws()
    {
        var family = NewTempDir();
        File.WriteAllText(Path.Combine(family, "3.yaml"),
            "id: 3\nname: PKG3\nlength: 1\nfields:\n- name: A\n  kind: Value\n  byte: 5\n");

        Assert.Throws<ConfigValidationException>(() => ProtocolLoader.LoadFamily(family));
    }

    [Test]
    public void Factory_ParsesSynchroTimestamp()
    {
        var factory = new CanPackageFactory();
        factory.LoadFrom(ProtocolLoader.LoadActiveFamily(TestConfig.Root()));

        var synchro = new CanPackage(PackageType.Standard, 1120, new byte[] { 25, 6, 18, 12, 30, 45 }, 0, 0);
        var parsed = factory.Create(synchro, new ParseContext());

        Assert.That(parsed.ParseData()!.Value.Messages[0], Is.EqualTo("18.06.2025 12:30:45"));
    }

    [Test]
    public void Factory_ParsesOffPwrConstantMessage()
    {
        var factory = new CanPackageFactory();
        factory.LoadFrom(ProtocolLoader.LoadActiveFamily(TestConfig.Root()));

        var offPwr = new CanPackage(PackageType.Standard, 1023, Array.Empty<byte>(), 0, 0);
        var data = factory.Create(offPwr, new ParseContext()).ParseData();

        Assert.That(data!.Value.Messages, Is.EqualTo(new[] { "выключения питания" }));
    }

    private static CanPackageFactory LoadedFactory()
    {
        var factory = new CanPackageFactory();
        factory.LoadFrom(ProtocolLoader.LoadActiveFamily(TestConfig.Root()));
        return factory;
    }

    private static System.Collections.Generic.List<string> Messages(CanPackageFactory factory, int id, byte[] data)
    {
        var pkg = new CanPackage(PackageType.Standard, id, data, 0, 0);
        return factory.Create(pkg, new ParseContext()).ParseData()!.Value.Messages;
    }

    [Test]
    public void Factory_StatErrMix_ParsesTwoNumerics()
    {
        var factory = LoadedFactory();
        Assert.That(factory.RegisteredIds, Contains.Item(1104));

        var pkg = new CanPackage(PackageType.Standard, 1104, new byte[] { 0x05, 0x00, 0x10, 0x00 }, 0, 0);
        var numeric = factory.Create(pkg, new ParseContext()).ParseData()!.Value.NumericData.ToList();
        Assert.That(numeric.Any(n => n.Name == "error_bit_number" && n.Value == 5), Is.True);
        Assert.That(numeric.Any(n => n.Name == "error_voltage_mV" && n.Value == 16), Is.True);
    }

    [Test]
    public void Factory_Par1_InspirationTimeSentinel()
    {
        var factory = LoadedFactory();
        var msgs = Messages(factory, 1187, new byte[] { 0, 0, 0, 0, 0xFF, 0xFF, 0, 0 });
        Assert.That(msgs, Contains.Item("Время вдоха: авто-режим"));
    }

    [Test]
    public void Factory_StatusScm_BitEmitsBothStates()
    {
        var factory = LoadedFactory();

        var off = Messages(factory, 1058, new byte[] { 0, 0, 0, 0 });
        Assert.That(off, Contains.Item("Нет активного подключения к сети Ethernet"));

        var on = Messages(factory, 1058, new byte[] { 0b0000_0010, 0, 0, 0 });
        Assert.That(on, Contains.Item("Есть активное подключение к сети Ethernet"));
    }

    [Test]
    public void Factory_OutExtflow_AvgCountZeroMessage()
    {
        var factory = LoadedFactory();
        var msgs = Messages(factory, 1345, new byte[] { 0, 0, 0 }); // avgCount (byte2 bits0-6) == 0
        Assert.That(msgs, Contains.Item("Данные не обновлены (используется предыдущая точка)"));
    }

    [Test]
    public void Factory_StatusPwr_OutputVoltageOffset()
    {
        var factory = LoadedFactory();
        var pkg = new CanPackage(PackageType.Standard, 1025, new byte[] { 50, 0, 0, 0, 0 }, 0, 0);
        var numeric = factory.Create(pkg, new ParseContext()).ParseData()!.Value.NumericData;
        Assert.That(numeric.Any(n => n.Name == "OutputVoltage" && System.Math.Abs(n.Value - 15) < 1e-9), Is.True);
    }

    [Test]
    public void Factory_Capno2_StatusGatedByPresence()
    {
        var factory = LoadedFactory();

        // presence (byte5) == 0x14 -> status bits suppressed, only presence message
        var disconnected = Messages(factory, 1219, new byte[] { 0x01, 0, 0, 0, 0, 0x14, 0, 0 });
        Assert.That(disconnected, Does.Not.Contain("Отрицательное значение CO2"));
        Assert.That(disconnected, Contains.Item("Капнограф не подключен"));

        // presence == 0 -> status bits parsed
        var connected = Messages(factory, 1219, new byte[] { 0x01, 0, 0, 0, 0, 0x00, 0, 0 });
        Assert.That(connected, Contains.Item("Отрицательное значение CO2"));
        Assert.That(connected, Contains.Item("Капнограф подключен"));
    }

    [Test]
    public void Factory_ContextFlow_ClbrErrUsesMode()
    {
        var factory = new CanPackageFactory();
        factory.LoadFrom(ProtocolLoader.LoadActiveFamily(TestConfig.Root()));
        var context = new ParseContext();

        var mode = new CanPackage(PackageType.Standard, 1184, new byte[] { 5 }, 0, 0);
        factory.Create(mode, context);
        Assert.That(context.CivlMode, Is.EqualTo(5));

        var clbr = new CanPackage(PackageType.Standard, 1193, new byte[] { 1, 100 }, 0, 0);
        var data = factory.Create(clbr, context).ParseData()!.Value;

        Assert.That(data.Messages, Contains.Item("калибровка успешно завершена"));
        Assert.That(data.NumericData.Any(n => n.Name == "Resource" && n.Value == 100), Is.True);
    }
}
