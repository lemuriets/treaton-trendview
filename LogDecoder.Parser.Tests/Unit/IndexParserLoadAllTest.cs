using System.Text.RegularExpressions;
using FluentAssertions;
using LogDecoder.CAN;
using LogDecoder.Parser;
using LogDecoder.Parser.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace LogDecoder.Parser.Tests.Unit;

[TestFixture]
public class IndexParserLoadAllTests
{
    private static readonly DateTime BaseTime = new(2025, 1, 1, 12, 0, 0);

    private string _logsDir = null!;
    private string _indexDir = null!;

    [SetUp]
    public void SetUp()
    {
        _logsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _indexDir = Path.Combine(_logsDir, "index");
        Directory.CreateDirectory(_indexDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_logsDir))
        {
            Directory.Delete(_logsDir, recursive: true);
        }
    }

    [Test]
    public void LoadAll_SameFileSessions_EndOffsetPointsAtFollowingSynchro()
    {
        var index = WriteIndexFile("00",
            (100, BaseTime),
            (200, BaseTime.AddSeconds(1)),
            (500, BaseTime.AddSeconds(100)),
            (600, BaseTime.AddSeconds(101)));

        var parser = LoadParser("00");
        parser.LoadAll(new[] { index });

        parser.Sessions.Count.Should().Be(2);

        var first = parser.Sessions[0];
        first.StartOffset.Should().Be(100);
        first.EndOffset.Should().Be(500); // start of the synchro that follows this session's last synchro
        first.Filenames.Should().Equal("00");

        var second = parser.Sessions[1];
        second.StartOffset.Should().Be(500);
        second.EndOffset.Should().BeNull(); // last session -> read to EOF
        second.Filenames.Should().Equal("00");
    }

    [Test]
    public void LoadAll_CrossFileContinuousSession_FilenamesSpanBothFiles_EndOffsetNull()
    {
        var index00 = WriteIndexFile("00",
            (100, BaseTime),
            (200, BaseTime.AddSeconds(1)));
        var index01 = WriteIndexFile("01",
            (50, BaseTime.AddSeconds(2)),
            (150, BaseTime.AddSeconds(3)));

        var parser = LoadParser("00", "01");
        parser.LoadAll(new[] { index00, index01 });

        parser.Sessions.Count.Should().Be(1);

        var session = parser.Sessions[0];
        session.Filenames.Should().Equal("00", "01");
        session.StartOffset.Should().Be(100);
        session.EndOffset.Should().BeNull();
    }

    [Test]
    public void LoadAll_NextSessionInDifferentFile_EndOffsetNull()
    {
        var index00 = WriteIndexFile("00",
            (100, BaseTime),
            (200, BaseTime.AddSeconds(1)));
        var index01 = WriteIndexFile("01",
            (50, BaseTime.AddSeconds(100)), // > 15s gap -> new session, different file
            (150, BaseTime.AddSeconds(101)));

        var parser = LoadParser("00", "01");
        parser.LoadAll(new[] { index00, index01 });

        parser.Sessions.Count.Should().Be(2);

        var first = parser.Sessions[0];
        first.Filenames.Should().Equal("00");
        first.EndOffset.Should().BeNull(); // following synchro is in file "01", not "00"
    }

    [Test]
    public void FindFloor_ReturnsGreatestEntryAtOrBeforeTarget()
    {
        var index = WriteIndexFile("00",
            (100, BaseTime),
            (200, BaseTime.AddSeconds(2)),
            (300, BaseTime.AddSeconds(4)));

        var parser = LoadParser("00");
        parser.LoadAll(new[] { index });
        var session = parser.Sessions[0];

        parser.FindFloor(session, BaseTime.AddSeconds(3)).Offset.Should().Be(200);
        parser.FindFloor(session, BaseTime.AddSeconds(4)).Offset.Should().Be(300);
    }

    [Test]
    public void FindFloor_TargetBeforeFirst_ReturnsFirstEntry()
    {
        var index = WriteIndexFile("00",
            (100, BaseTime),
            (200, BaseTime.AddSeconds(2)));

        var parser = LoadParser("00");
        parser.LoadAll(new[] { index });
        var session = parser.Sessions[0];

        parser.FindFloor(session, BaseTime.AddSeconds(-10)).Offset.Should().Be(100);
    }

    [Test]
    public void FindCeiling_ReturnsSmallestEntryAfterTarget()
    {
        var index = WriteIndexFile("00",
            (100, BaseTime),
            (200, BaseTime.AddSeconds(2)),
            (300, BaseTime.AddSeconds(4)));

        var parser = LoadParser("00");
        parser.LoadAll(new[] { index });
        var session = parser.Sessions[0];

        parser.FindCeiling(session, BaseTime.AddSeconds(3))!.Value.Offset.Should().Be(300);
    }

    [Test]
    public void FindCeiling_TargetAtOrAfterLast_ReturnsNull()
    {
        var index = WriteIndexFile("00",
            (100, BaseTime),
            (200, BaseTime.AddSeconds(2)));

        var parser = LoadParser("00");
        parser.LoadAll(new[] { index });
        var session = parser.Sessions[0];

        parser.FindCeiling(session, BaseTime.AddSeconds(2)).Should().BeNull();
        parser.FindCeiling(session, BaseTime.AddSeconds(10)).Should().BeNull();
    }

    private IndexParser LoadParser(params string[] dataFiles)
    {
        foreach (var name in dataFiles)
        {
            File.WriteAllText(Path.Combine(_logsDir, name), string.Empty);
        }
        var aggregator = new LogFilesAggregator(_logsDir, Path.GetFileName, new Regex(@"^[0-1][0-9]$"));
        return new IndexParser(NullLogger.Instance, aggregator);
    }

    private string WriteIndexFile(string sourceName, params (long offset, DateTime time)[] entries)
    {
        var path = Path.Combine(_indexDir, sourceName + ".txt");
        var lines = new List<string> { $"FormatVersion 2; SourceFileName {sourceName}" };
        foreach (var (offset, time) in entries)
        {
            var bufNum = offset / LogBuffer.BufferWithHeaderSize;
            var inBuffer = offset % LogBuffer.BufferWithHeaderSize;
            lines.Add($"{bufNum} {inBuffer} {time.ToString(CanConfig.TimeFormat)}");
        }
        File.WriteAllLines(path, lines);
        return path;
    }
}
