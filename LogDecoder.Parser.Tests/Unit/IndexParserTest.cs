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
    public void BuildSessions_SingleEntry_ReturnsOneGroup()
    {
        var entries = new[] { Entry(0) };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(1);
        result[0].Should().HaveCount(1);
        result[0][0].Time.Should().Be(BaseTime);
    }

    [Test]
    public void BuildSessions_OneSecondApart_StaysInSameGroup()
    {
        var entries = new[] { Entry(0), Entry(1), Entry(2) };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(1);
        result[0].Should().HaveCount(3);
        result[0][0].Time.Should().Be(BaseTime);
        result[0][^1].Time.Should().Be(BaseTime.AddSeconds(2));
    }

    [Test]
    public void BuildSessions_GapBelow15s_StaysInSameGroup()
    {
        var entries = new[] { Entry(0), Entry(14) };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(1);
        result[0].Should().HaveCount(2);
    }

    [Test]
    public void BuildSessions_GapExactly15s_SplitsGroup()
    {
        var entries = new[] { Entry(0), Entry(15) };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(2);
        result[0].Should().ContainSingle(e => e.Time == BaseTime);
        result[1].Should().ContainSingle(e => e.Time == BaseTime.AddSeconds(15));
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
        result[0].Should().HaveCount(3);
        result[1].Should().HaveCount(3);
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
        result[0].Should().HaveCount(3);
        result[0][0].Time.Should().Be(BaseTime.AddSeconds(1000));
        result[1].Should().HaveCount(3);
        result[1][0].Time.Should().Be(BaseTime);
        result[2].Should().HaveCount(2);
        result[2][0].Time.Should().Be(BaseTime.AddSeconds(2000));
    }

    [Test]
    public void BuildSessions_BackwardJumpBelowThreshold_StaysInSameGroup()
    {
        var entries = new[]
        {
            Entry(20), Entry(21), Entry(22),
            Entry(15),
        };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(1);
        result[0].Should().HaveCount(4);
    }

    [Test]
    public void BuildSessions_CrossFileContinuous_KeepsSingleGroup()
    {
        var entries = new[]
        {
            Entry(0, "00"), Entry(1, "00"), Entry(2, "00"),
            Entry(3, "01"), Entry(4, "01"),
        };

        var result = IndexParser.BuildSessions(entries).ToList();

        result.Should().HaveCount(1);
        result[0].Should().HaveCount(5);
        result[0].Select(e => e.Filename).Should().Equal("00", "00", "00", "01", "01");
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
        result[0].Select(e => e.Filename).Should().Equal("00", "00");
        result[1].Select(e => e.Filename).Should().Equal("01", "01");
    }
}
