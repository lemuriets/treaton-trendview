using FluentAssertions;
using LogDecoder.Parser.Data;

namespace LogDecoder.Parser.Tests.Unit;

[TestFixture]
public class TimeRangeTests
{
    [Test]
    public void Ctor_Should_Set_Start_And_End()
    {
        var start = new DateTime(2026, 1, 1, 10, 0, 0);
        var end = new DateTime(2026, 1, 1, 11, 0, 0);

        var range = new TimeRange(start, end);

        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Test]
    public void Duration_Should_Return_Difference_Between_End_And_Start()
    {
        var start = new DateTime(2026, 1, 1, 10, 0, 0);
        var end = new DateTime(2026, 1, 1, 12, 30, 0);

        var range = new TimeRange(start, end);

        range.Duration.Should().Be(TimeSpan.FromHours(2.5));
    }

    [Test]
    public void Ctor_Should_Throw_When_Start_Equals_End()
    {
        var time = new DateTime(2026, 1, 1, 10, 0, 0);

        Action act = () => new TimeRange(time, time);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("from must be < to");
    }

    [Test]
    public void Ctor_Should_Throw_When_Start_Is_Greater_Than_End()
    {
        var start = new DateTime(2026, 1, 1, 11, 0, 0);
        var end = new DateTime(2026, 1, 1, 10, 0, 0);

        Action act = () => new TimeRange(start, end);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("from must be < to");
    }

    [Test]
    public void Contains_Should_Return_True_For_Time_Inside_Range()
    {
        var start = new DateTime(2026, 1, 1, 10, 0, 0);
        var end = new DateTime(2026, 1, 1, 11, 0, 0);
        var range = new TimeRange(start, end);

        var value = new DateTime(2026, 1, 1, 10, 30, 0);

        var result = range.Contains(value);

        result.Should().BeTrue();
    }

    [Test]
    public void Contains_Should_Return_True_When_Time_Equals_Start()
    {
        var start = new DateTime(2026, 1, 1, 10, 0, 0);
        var end = new DateTime(2026, 1, 1, 11, 0, 0);
        var range = new TimeRange(start, end);

        var result = range.Contains(start);

        result.Should().BeTrue();
    }

    [Test]
    public void Contains_Should_Return_False_When_Time_Equals_End()
    {
        var start = new DateTime(2026, 1, 1, 10, 0, 0);
        var end = new DateTime(2026, 1, 1, 11, 0, 0);
        var range = new TimeRange(start, end);

        var result = range.Contains(end);

        result.Should().BeFalse();
    }

    [Test]
    public void Contains_Should_Return_False_When_Time_Is_Before_Start()
    {
        var start = new DateTime(2026, 1, 1, 10, 0, 0);
        var end = new DateTime(2026, 1, 1, 11, 0, 0);
        var range = new TimeRange(start, end);

        var value = start.AddTicks(-1);

        var result = range.Contains(value);

        result.Should().BeFalse();
    }

    [Test]
    public void Contains_Should_Return_False_When_Time_Is_After_End()
    {
        var start = new DateTime(2026, 1, 1, 10, 0, 0);
        var end = new DateTime(2026, 1, 1, 11, 0, 0);
        var range = new TimeRange(start, end);

        var value = end.AddTicks(1);

        var result = range.Contains(value);

        result.Should().BeFalse();
    }

    [Test]
    public void Contains_Should_Handle_Minimal_Valid_Range()
    {
        var start = new DateTime(2026, 1, 1, 10, 0, 0);
        var end = start.AddTicks(1);

        var range = new TimeRange(start, end);

        range.Contains(start).Should().BeTrue();
        range.Contains(end).Should().BeFalse();
    }
}