using System.Text.RegularExpressions;
using FluentAssertions;
using LogDecoder.Parser;
using LogDecoder.Parser.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace LogDecoder.Parser.Tests.Unit;

[TestFixture]
public class IndexParserSmokeTests
{
    private const string FixtureRelativePath = "problem_log_test/index/00.txt";

    private static readonly DateTime WindowStart = new(2025, 10, 16, 17, 36, 21);
    private static readonly DateTime WindowEnd = new(2025, 10, 16, 18, 9, 30);
    private static readonly DateTime FirstEntryTime = new(2025, 11, 18, 2, 11, 27);
    private static readonly DateTime LastEntryTime = new(2026, 3, 16, 23, 38, 1);
    private const long WindowFollowingSynchroOffset = 1172734238L;

    [Test]
    public void LoadAll_RealIndexFile_DetectsWindowSessionAndChronologicalOrder()
    {
        var fixturePath = TryFindFixture();
        if (fixturePath is null)
        {
            Assert.Inconclusive($"Fixture not found: {FixtureRelativePath}");
        }

        var logsFolder = Path.GetDirectoryName(Path.GetDirectoryName(fixturePath))!;
        var aggregator = new LogFilesAggregator(logsFolder, Path.GetFileName, new Regex(@"^[0-1][0-9]$"));

        var parser = new IndexParser(NullLogger.Instance, aggregator);
        parser.LoadAll(new[] { fixturePath });

        parser.Sessions.Count.Should().Be(49);
        parser.MinTime.Should().Be(WindowStart);
        parser.MaxTime.Should().Be(LastEntryTime);

        var window = parser.Sessions[0];
        window.StartDT.Should().Be(WindowStart);
        window.EndDT.Should().Be(WindowEnd);
        window.Filenames.Should().Equal("00");
        window.EndOffset.Should().Be(WindowFollowingSynchroOffset);

        parser.Sessions[1].StartDT.Should().Be(FirstEntryTime);

        for (var i = 1; i < parser.Sessions.Count; i++)
        {
            parser.Sessions[i].StartDT.Should().BeAfter(parser.Sessions[i - 1].StartDT);
        }
    }

    private static string? TryFindFixture()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, FixtureRelativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
