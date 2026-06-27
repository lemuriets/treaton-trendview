using FluentAssertions;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.Packages;
using LogDecoder.CAN.Protocol;
using LogDecoder.Parser;
using LogDecoder.Parser.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;

namespace LogDecoder.Parser.Tests.Unit;

[TestFixture]
public class LogParserIntegrationTests
{
    private string _tempDir = null!;
    private LogParser _parser = null!;

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public void GetPackages_SingleSession_ReturnsAllSynchros()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 0);
        WriteLogFile("00",
            LogFixture.Buffer(
                LogFixture.SynchroPackage(t),
                LogFixture.SynchroPackage(t.AddSeconds(1)),
                LogFixture.SynchroPackage(t.AddSeconds(2))));

        InitParser();

        var packages = _parser
            .GetPackages(SynchroOnly(), t, t.AddSeconds(2))
            .ToList();

        packages.Should().HaveCount(3);
        packages.Should().AllSatisfy(p => p.Id.Should().Be(LogFixture.SynchroId));
    }

    [Test]
    public void GetPackages_Window_ReturnsOnlyWindowPackages_NotSurroundingNewerData()
    {
        var newer = new DateTime(2025, 6, 1, 10, 0, 0);
        var older = new DateTime(2025, 1, 1, 10, 0, 0);
        var newerTail = new DateTime(2025, 6, 1, 11, 0, 0);

        WriteLogFile("00",
            LogFixture.Buffer(
                LogFixture.SynchroPackage(newer),
                LogFixture.SynchroPackage(newer.AddSeconds(1)),
                LogFixture.SynchroPackage(newer.AddSeconds(2)),
                LogFixture.SynchroPackage(older),
                LogFixture.SynchroPackage(older.AddSeconds(1)),
                LogFixture.SynchroPackage(older.AddSeconds(2)),
                LogFixture.SynchroPackage(newerTail),
                LogFixture.SynchroPackage(newerTail.AddSeconds(1))));

        InitParser();

        var windowPackages = _parser
            .GetPackages(SynchroOnly(), older, older.AddSeconds(2))
            .ToList();

        windowPackages.Should().HaveCount(3);

        var newerHeadPackages = _parser
            .GetPackages(SynchroOnly(), newer, newer.AddSeconds(2))
            .ToList();

        newerHeadPackages.Should().HaveCount(3);

        var newerTailPackages = _parser
            .GetPackages(SynchroOnly(), newerTail, newerTail.AddSeconds(1))
            .ToList();

        newerTailPackages.Should().HaveCount(2);
    }

    [Test]
    public void GetPackages_CrossFileSession_ReturnsPackagesFromBothFiles()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 0);
        WriteLogFile("00",
            LogFixture.Buffer(
                LogFixture.SynchroPackage(t),
                LogFixture.SynchroPackage(t.AddSeconds(1))));
        WriteLogFile("01",
            LogFixture.Buffer(
                LogFixture.SynchroPackage(t.AddSeconds(2)),
                LogFixture.SynchroPackage(t.AddSeconds(3))));

        InitParser();

        var packages = _parser
            .GetPackages(SynchroOnly(), t, t.AddSeconds(3))
            .ToList();

        packages.Should().HaveCount(4);
        _parser.MinDatetime.Should().Be(t);
        _parser.MaxDatetime.Should().Be(t.AddSeconds(3));
    }

    [Test]
    public void GetPackages_PartialRange_ClipsByFloorAndCeiling()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 0);
        WriteLogFile("00",
            LogFixture.Buffer(
                LogFixture.SynchroPackage(t),
                LogFixture.SynchroPackage(t.AddSeconds(1)),
                LogFixture.SynchroPackage(t.AddSeconds(2)),
                LogFixture.SynchroPackage(t.AddSeconds(3)),
                LogFixture.SynchroPackage(t.AddSeconds(4))));

        InitParser();

        var packages = _parser
            .GetPackages(SynchroOnly(), t.AddSeconds(1), t.AddSeconds(3))
            .ToList();

        packages.Should().HaveCount(3);
    }

    [Test]
    public void GetPackages_CrossFileSession_StartInSecondFile_DoesNotReturnEarlierFile()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 0);
        WriteLogFile("00",
            LogFixture.Buffer(
                LogFixture.SynchroPackage(t),
                LogFixture.SynchroPackage(t.AddSeconds(1))));
        WriteLogFile("01",
            LogFixture.Buffer(
                LogFixture.SynchroPackage(t.AddSeconds(2)),
                LogFixture.SynchroPackage(t.AddSeconds(3))));

        InitParser();

        var packages = _parser
            .GetPackages(SynchroOnly(), t.AddSeconds(2), t.AddSeconds(3))
            .ToList();

        packages.Should().HaveCount(2);
    }

    [Test]
    public void GetPackages_RangeOutsideAllSessions_ReturnsEmpty()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 0);
        WriteLogFile("00",
            LogFixture.Buffer(
                LogFixture.SynchroPackage(t),
                LogFixture.SynchroPackage(t.AddSeconds(1))));

        InitParser();

        var packages = _parser
            .GetPackages(SynchroOnly(), t.AddDays(1), t.AddDays(1).AddSeconds(10))
            .ToList();

        packages.Should().BeEmpty();
    }

    [Test]
    public void GetPackages_FilterIncludesMultipleIds_ReturnsAllMatching()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 0);
        WriteLogFile("00",
            LogFixture.Buffer(
                LogFixture.SynchroPackage(t),
                LogFixture.OxyPackage(1, 2),
                LogFixture.OxyPackage(3, 4),
                LogFixture.SynchroPackage(t.AddSeconds(1)),
                LogFixture.OxyPackage(5, 6)));

        InitParser();

        var filter = new HashSet<int> { LogFixture.SynchroId, LogFixture.OxyId };
        var packages = _parser
            .GetPackages(filter, t, t.AddSeconds(1))
            .ToList();

        packages.Select(p => p.Id).Should().Equal(
            LogFixture.SynchroId,
            LogFixture.OxyId,
            LogFixture.OxyId,
            LogFixture.SynchroId,
            LogFixture.OxyId);
    }

    private void InitParser()
    {
        var factory = new CanPackageFactory();
        factory.LoadFrom(ProtocolLoader.LoadActiveFamily(FindConfigRoot()));
        _parser = new LogParser(NullLogger.Instance, _tempDir, factory);
        _parser.CreateOrLoadAllIndexes();
    }

    private static string FindConfigRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "config");
            if (Directory.Exists(candidate) && Directory.GetDirectories(candidate).Length > 0)
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Root config/ folder not found.");
    }

    private void WriteLogFile(string filename, params byte[][] buffers)
    {
        LogFixture.WriteFile(Path.Combine(_tempDir, filename), buffers);
    }

    private static IReadOnlySet<int> SynchroOnly()
    {
        return new HashSet<int> { LogFixture.SynchroId };
    }
}
