using FluentAssertions;
using LogDecoder.Parser.Data;

namespace LogDecoder.Parser.Tests.Unit;

[TestFixture]
public class LogSessionsSortedTests
{
    private LogSessionsSorted _storage;

    [SetUp]
    public void Setup()
    {
        _storage = new LogSessionsSorted();
    }

    private static LogSession CreateSession(int startMin, int endMin)
    {
        var start = new DateTime(2026, 1, 1).AddMinutes(startMin);
        var end = new DateTime(2026, 1, 1).AddMinutes(endMin);

        return new LogSession(0, null, start, end, new[] { "00" });
    }

    [Test]
    public void Add_FirstSession_ShouldSucceed()
    {
        var session = CreateSession(0, 10);

        _storage.Add(session);

        _storage.Count.Should().Be(1);
    }

    [Test]
    public void Add_SequentialSessions_ShouldSucceed()
    {
        _storage.Add(CreateSession(0, 10));
        _storage.Add(CreateSession(10, 20));

        _storage.Count.Should().Be(2);
    }

    [Test]
    public void Add_OverlappingWithPrevious_ShouldThrow()
    {
        _storage.Add(CreateSession(0, 10));

        Action act = () => _storage.Add(CreateSession(9, 15));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*previous*");
    }

    [Test]
    public void Add_OverlappingWithNext_ShouldThrow()
    {
        _storage.Add(CreateSession(10, 20));

        var act = () => _storage.Add(CreateSession(5, 15));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*next*");
    }

    [Test]
    public void Add_BoundaryTouching_ShouldBeValid()
    {
        _storage.Add(CreateSession(0, 10));

        var act = () => _storage.Add(CreateSession(10, 20));

        act.Should().NotThrow();
    }

    [Test]
    public void Contains_WhenInsideSession_ShouldReturnTrue()
    {
        _storage.Add(CreateSession(0, 10));

        var result = _storage.Contains(new DateTime(2026, 1, 1).AddMinutes(5));

        result.Should().BeTrue();
    }

    [Test]
    public void Contains_WhenOutsideSession_ShouldReturnFalse()
    {
        _storage.Add(CreateSession(0, 10));

        var result = _storage.Contains(new DateTime(2026, 1, 1).AddMinutes(15));

        result.Should().BeFalse();
    }

    [Test]
    public void GetSessionByTime_OnStartBoundary_ShouldReturnSession()
    {
        var session = CreateSession(0, 10);

        _storage.Add(session);

        var result = _storage.GetSessionByTime(session.StartDT);

        result.Should().NotBeNull();
    }

    [Test]
    public void GetSessionByTime_OnEndBoundary_ShouldReturnSession()
    {
        var session = CreateSession(0, 10);

        _storage.Add(session);

        var result = _storage.GetSessionByTime(session.EndDT);

        result.Should().NotBeNull();
    }

    [Test]
    public void GetRange_ShouldReturnAllOverlappingSessions()
    {
        _storage.Add(CreateSession(0, 10));
        _storage.Add(CreateSession(15, 25));
        _storage.Add(CreateSession(30, 40));

        var range = new TimeRange(
            new DateTime(2026, 1, 1).AddMinutes(5),
            new DateTime(2026, 1, 1).AddMinutes(35));

        var result = _storage.GetRange(range);

        result.Should().HaveCount(3);
    }

    [Test]
    public void GetRange_ShouldReturnSingleSession_WhenFullyInside()
    {
        _storage.Add(CreateSession(0, 10));
        _storage.Add(CreateSession(20, 30));

        var range = new TimeRange(
            new DateTime(2026, 1, 1).AddMinutes(21),
            new DateTime(2026, 1, 1).AddMinutes(22));

        var result = _storage.GetRange(range);

        result.Should().HaveCount(1);
    }

    [Test]
    public void GetRange_ShouldReturnEmpty_WhenNoOverlap()
    {
        _storage.Add(CreateSession(0, 10));
        _storage.Add(CreateSession(20, 30));

        var range = new TimeRange(
            new DateTime(2026, 1, 1).AddMinutes(11),
            new DateTime(2026, 1, 1).AddMinutes(19));

        var result = _storage.GetRange(range);

        result.Should().BeEmpty();
    }

    [Test]
    public void GetRange_ShouldIncludeBoundaryOverlaps()
    {
        _storage.Add(CreateSession(0, 10));
        _storage.Add(CreateSession(10, 20));
        _storage.Add(CreateSession(20, 30));

        var range = new TimeRange(
            new DateTime(2026, 1, 1).AddMinutes(10),
            new DateTime(2026, 1, 1).AddMinutes(20));

        var result = _storage.GetRange(range);

        result.Should().HaveCount(3);
    }

    [Test]
    public void GetRange_ShouldReturnSessions_WhenRangeCoversAll()
    {
        _storage.Add(CreateSession(0, 10));
        _storage.Add(CreateSession(10, 20));
        _storage.Add(CreateSession(20, 30));

        var range = new TimeRange(
            new DateTime(2026, 1, 1).AddMinutes(0),
            new DateTime(2026, 1, 1).AddMinutes(60));

        var result = _storage.GetRange(range);

        result.Should().HaveCount(3);
    }
}