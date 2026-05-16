using FluentAssertions;
using LogDecoder.Parser.Data;

namespace LogDecoder.Parser.Tests.Unit;

[TestFixture]
public class IndexParserBuildSessionsTests
{
    private static readonly DateTime BaseTime = new(2026, 1, 1, 12, 0, 0);

    private static IndexEntry Entry(int secondsFromBase, string filename = "00", long offset = 0)
    {
        return new IndexEntry(filename, offset, BaseTime.AddSeconds(secondsFromBase));
    }

    [Test]
    public void BuildSessions_Empty_ReturnsNoSessions()
    {
        var result = IndexParser.BuildSessions(Array.Empty<IndexEntry>()).ToList();

        result.Should().BeEmpty();
    }

    [Test]
    public void BuildSessions_SingleEntry_ReturnsOneSession_WithSyntheticOneSecondRange()
    {
        var entries = new[] { Entry(0) };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(1);
        result[0].Indexes.Should().HaveCount(1);
        result[0].Start.Should().Be(BaseTime);
        result[0].End.Should().Be(BaseTime.AddSeconds(1));
    }

    [Test]
    public void BuildSessions_OneSecondApart_StaysInSameSession()
    {
        var entries = new[] { Entry(0), Entry(1), Entry(2) };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(1);
        result[0].Indexes.Should().HaveCount(3);
        result[0].Start.Should().Be(BaseTime);
        result[0].End.Should().Be(BaseTime.AddSeconds(2));
    }

    [Test]
    public void BuildSessions_GapBelow15s_StaysInSameSession()
    {
        var entries = new[] { Entry(0), Entry(14) };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(1);
        result[0].Indexes.Should().HaveCount(2);
    }

    [Test]
    public void BuildSessions_GapExactly15s_SplitsSession()
    {
        var entries = new[] { Entry(0), Entry(15) };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(2);
        result[0].Indexes.Should().ContainSingle(e => e.Time == BaseTime);
        result[1].Indexes.Should().ContainSingle(e => e.Time == BaseTime.AddSeconds(15));
    }

    [Test]
    public void BuildSessions_ForwardGap_Splits()
    {
        var entries = new[]
        {
            Entry(0), Entry(1), Entry(2),
            Entry(100),
            Entry(101), Entry(102),
        };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(2);
        result[0].Indexes.Should().HaveCount(3);
        result[1].Indexes.Should().HaveCount(3);
    }

    [Test]
    public void BuildSessions_BackwardJump_BeyondThreshold_Splits_ProducesWindow()
    {
        var entries = new[]
        {
            Entry(1000), Entry(1001), Entry(1002),
            Entry(0), Entry(1), Entry(2),
            Entry(2000), Entry(2001),
        };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(3);
        result[0].Indexes.Should().HaveCount(3);
        result[0].Start.Should().Be(BaseTime.AddSeconds(1000));
        result[1].Indexes.Should().HaveCount(3);
        result[1].Start.Should().Be(BaseTime);
        result[2].Indexes.Should().HaveCount(2);
        result[2].Start.Should().Be(BaseTime.AddSeconds(2000));
    }

    [Test]
    public void BuildSessions_BackwardJumpBelowThreshold_StaysInSameSession()
    {
        var entries = new[]
        {
            Entry(20), Entry(21), Entry(22),
            Entry(15),
        };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(1);
        result[0].Indexes.Should().HaveCount(4);
    }

    [Test]
    public void BuildSessions_CrossFileContinuous_KeepsSingleSession()
    {
        var entries = new[]
        {
            Entry(0, "00"), Entry(1, "00"), Entry(2, "00"),
            Entry(3, "01"), Entry(4, "01"),
        };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(1);
        result[0].Indexes.Should().HaveCount(5);
        result[0].Indexes.Select(e => e.Filename).Should().Equal("00", "00", "00", "01", "01");
    }

    [Test]
    public void BuildSessions_CrossFileWithGap_Splits()
    {
        var entries = new[]
        {
            Entry(0, "00"), Entry(1, "00"),
            Entry(100, "01"), Entry(101, "01"),
        };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(2);
        result[0].Indexes.Select(e => e.Filename).Should().Equal("00", "00");
        result[1].Indexes.Select(e => e.Filename).Should().Equal("01", "01");
    }

    [Test]
    public void BuildSessions_MultipleWindows_SortedChronologicallyByLogSessionsSorted()
    {
        var entries = new[]
        {
            Entry(5000), Entry(5001), Entry(5002),
            Entry(0), Entry(1), Entry(2),
            Entry(8000), Entry(8001),
            Entry(2000), Entry(2001),
        };

        var sessions = IndexParser.BuildSessions(entries).ToList();
        var sorted = new LogSessionsSorted();
        foreach (var session in sessions)
        {
            sorted.Add(session);
        }

        sorted.Count.Should().Be(4);
        sorted[0].Start.Should().Be(BaseTime);
        sorted[1].Start.Should().Be(BaseTime.AddSeconds(2000));
        sorted[2].Start.Should().Be(BaseTime.AddSeconds(5000));
        sorted[3].Start.Should().Be(BaseTime.AddSeconds(8000));
    }

    [Test]
    public void BuildSessions_OffsetsPreservedWithinSession()
    {
        var entries = new[]
        {
            new IndexEntry("00", 100, BaseTime),
            new IndexEntry("00", 200, BaseTime.AddSeconds(1)),
            new IndexEntry("01", 300, BaseTime.AddSeconds(2)),
        };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(1);
        result[0].StartOffset.Should().Be(100);
        result[0].EndOffset.Should().Be(300);
    }

    [Test]
    public void BuildSessions_PhysicalEndOffset_SetWhenNextSessionInSameFile()
    {
        var entries = new[]
        {
            new IndexEntry("00", 100, BaseTime),
            new IndexEntry("00", 200, BaseTime.AddSeconds(1)),
            new IndexEntry("00", 500, BaseTime.AddSeconds(100)),
            new IndexEntry("00", 600, BaseTime.AddSeconds(101)),
        };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(2);
        result[0].PhysicalEndOffsetInLastFile.Should().Be(500);
        result[1].PhysicalEndOffsetInLastFile.Should().BeNull();
    }

    [Test]
    public void BuildSessions_PhysicalEndOffset_NullWhenNextSessionInDifferentFile()
    {
        var entries = new[]
        {
            new IndexEntry("00", 100, BaseTime),
            new IndexEntry("00", 200, BaseTime.AddSeconds(1)),
            new IndexEntry("01", 50, BaseTime.AddSeconds(100)),
            new IndexEntry("01", 100, BaseTime.AddSeconds(101)),
        };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(2);
        result[0].PhysicalEndOffsetInLastFile.Should().BeNull();
        result[1].PhysicalEndOffsetInLastFile.Should().BeNull();
    }

    [Test]
    public void BuildSessions_PhysicalEndOffset_WindowSandwich()
    {
        var entries = new[]
        {
            new IndexEntry("00", 100, BaseTime.AddSeconds(5000)),
            new IndexEntry("00", 200, BaseTime.AddSeconds(5001)),
            new IndexEntry("00", 300, BaseTime),
            new IndexEntry("00", 400, BaseTime.AddSeconds(1)),
            new IndexEntry("00", 500, BaseTime.AddSeconds(10000)),
            new IndexEntry("00", 600, BaseTime.AddSeconds(10001)),
        };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(3);
        result[0].PhysicalEndOffsetInLastFile.Should().Be(300);
        result[1].PhysicalEndOffsetInLastFile.Should().Be(500);
        result[2].PhysicalEndOffsetInLastFile.Should().BeNull();
    }
}
