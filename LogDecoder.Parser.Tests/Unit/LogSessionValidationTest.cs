using FluentAssertions;
using LogDecoder.Parser.Data;

namespace LogDecoder.Parser.Tests.Unit;

[TestFixture]
public class LogSessionValidationTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 12, 0, 0);
    private static readonly DateTime End = Start.AddMinutes(1);

    [Test]
    public void Ctor_ValidArguments_ExposesAllFields()
    {
        var session = new LogSession(100, 500, Start, End, new[] { "00", "01" });

        session.StartOffset.Should().Be(100);
        session.EndOffset.Should().Be(500);
        session.StartDT.Should().Be(Start);
        session.EndDT.Should().Be(End);
        session.Filenames.Should().Equal("00", "01");
    }

    [Test]
    public void Ctor_NullEndOffset_IsAllowed()
    {
        var act = () => new LogSession(100, null, Start, End, new[] { "00" });

        act.Should().NotThrow();
    }

    [Test]
    public void Ctor_EmptyFilenames_Throws()
    {
        var act = () => new LogSession(0, null, Start, End, Array.Empty<string>());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Filenames*");
    }

    [Test]
    public void Ctor_StartDtAfterEndDt_Throws()
    {
        var act = () => new LogSession(0, null, End, Start, new[] { "00" });

        act.Should().Throw<ArgumentException>()
            .WithMessage("*StartDT*");
    }

    [Test]
    public void Ctor_EqualStartAndEndDt_IsAllowed()
    {
        var act = () => new LogSession(0, null, Start, Start, new[] { "00" });

        act.Should().NotThrow();
    }

    [Test]
    public void Ctor_NegativeStartOffset_Throws()
    {
        var act = () => new LogSession(-1, null, Start, End, new[] { "00" });

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Ctor_NegativeEndOffset_Throws()
    {
        var act = () => new LogSession(0, -1, Start, End, new[] { "00" });

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Ctor_SingleFile_StartOffsetGreaterThanEndOffset_Throws()
    {
        var act = () => new LogSession(500, 100, Start, End, new[] { "00" });

        act.Should().Throw<ArgumentException>()
            .WithMessage("*single file*");
    }

    [Test]
    public void Ctor_MultiFile_StartOffsetGreaterThanEndOffset_IsAllowed()
    {
        // Offsets are per-file: StartOffset lives in the first file, EndOffset in the last,
        // so a larger start offset than end offset is legitimate across files.
        var act = () => new LogSession(500, 100, Start, End, new[] { "00", "01" });

        act.Should().NotThrow();
    }

    [Test]
    public void Contains_WithinRange_ReturnsTrue()
    {
        var session = new LogSession(0, null, Start, End, new[] { "00" });

        session.Contains(Start.AddSeconds(30)).Should().BeTrue();
        session.Contains(Start).Should().BeTrue();
        session.Contains(End).Should().BeTrue();
    }

    [Test]
    public void Contains_OutsideRange_ReturnsFalse()
    {
        var session = new LogSession(0, null, Start, End, new[] { "00" });

        session.Contains(Start.AddSeconds(-1)).Should().BeFalse();
        session.Contains(End.AddSeconds(1)).Should().BeFalse();
    }
}
