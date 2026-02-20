using LogDecoder.Parser.Data;

namespace LogDecoder.Parser.Tests.Unit;

[TestFixture]
public class JournalParserTests
{
    [Test]
    public void Parse_ValidLogLines_ReturnsCorrectSequence()
    {
        var lines = new[]
        {
            "log_start 12:00:00 01.01.2020 1 100",
            "log_stop 12:30:00 01.01.2020 1 150",
            "log_start 13:00:00 01.01.2020 2 200",
            "log_stop 13:30:00 01.01.2020 2 250"
        };

        var parser = new JournalParser();
        var sequence = parser.Parse(lines);

        Assert.That(sequence.TotalSeconds / 1800, Is.EqualTo(2)); // каждая сессия 30 минут
    }

    [Test]
    public void Parse_EmptyOrInvalidLines_Ignored()
    {
        var lines = new[]
        {
            "",
            "invalid line",
            "log_start 12:00:00 01.01.2020 1 100",
            "log_stop 12:30:00 01.01.2020 1 150"
        };

        var parser = new JournalParser();
        var sequence = parser.Parse(lines);

        Assert.That(sequence.TotalSeconds, Is.EqualTo(1800)); // только одна корректная сессия
    }

    [Test]
    public void BuildSequence_StopBeforeStart_Ignored()
    {
        var lines = new[]
        {
            "log_stop 12:30:00 01.01.2020 1 100",
            "log_start 12:00:00 01.01.2020 1 50"
        };

        var parser = new JournalParser();
        var sequence = parser.Parse(lines);

        Assert.That(sequence.TotalSeconds, Is.EqualTo(0)); // неверный порядок — игнор
    }

    [Test]
    public void LogSessionsSequence_AddAndTryAdd_WorksCorrectly()
    {
        var sequence = new LogSessionsSequence();

        var s1 = new LogSession(1, 1, new TimeRange(new DateTime(2020, 1, 1, 12, 0, 0), new DateTime(2020, 1, 1, 12, 30, 0)));
        var s2 = new LogSession(2, 2, new TimeRange(new DateTime(2020, 1, 1, 12, 30, 0), new DateTime(2020, 1, 1, 13, 0, 0)));
        var s3 = new LogSession(3, 3, new TimeRange(new DateTime(2020, 1, 1, 12, 15, 0), new DateTime(2020, 1, 1, 12, 45, 0))); // пересекается с s1

        // обычное добавление
        sequence.Add(s1);
        sequence.Add(s2);

        Assert.That(sequence.TotalSeconds, Is.EqualTo(3600));

        // TryAdd для пересекающейся сессии
        var result = sequence.TryAdd(s3);
        Assert.IsFalse(result);
        Assert.That(sequence.TotalSeconds, Is.EqualTo(3600)); // TotalSeconds не увеличилось
    }

    [Test]
    public void LogSessionsSequence_GetSessionByTime_ReturnsCorrectSession()
    {
        var sequence = new LogSessionsSequence();
        var s1 = new LogSession(1, 1, new TimeRange(new DateTime(2020, 1, 1, 12, 0, 0), new DateTime(2020, 1, 1, 12, 30, 0)));
        sequence.Add(s1);

        var target = new DateTime(2020, 1, 1, 12, 15, 0);
        var session = sequence.GetSessionByTime(target);

        Assert.IsNotNull(session);
        Assert.That(session, Is.EqualTo(s1));

        var outside = new DateTime(2020, 1, 1, 13, 0, 0);
        var none = sequence.GetSessionByTime(outside);
        Assert.IsNull(none);
    }
}