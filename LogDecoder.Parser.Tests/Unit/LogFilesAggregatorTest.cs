using System.Text.RegularExpressions;
using FluentAssertions;

namespace LogDecoder.Parser.Tests.Unit;

[TestFixture]
public class LogFilesAggregatorTests
{
    private readonly Regex _ivlLogFilenameTemplate = new Regex(@"^[0-1][0-9]$");
    private string _tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Test]
    public void Constructor_ShouldThrow_WhenDirectoryDoesNotExist()
    {
        var act = () => new LogFilesAggregator(
            "non-existent-folder",
            path => path,
            _ivlLogFilenameTemplate);

        act.Should()
            .Throw<DirectoryNotFoundException>();
    }

    [Test]
    public void Constructor_ShouldFilterAndSortFiles()
    {
        CreateFile("03");
        CreateFile("01");
        CreateFile("02");
        CreateFile("ignored.txt");

        var aggregator = CreateAggregator();

        aggregator.SortedFilenames.Should().Equal(
            "01",
            "02",
            "03");
    }

    [Test]
    public void Constructor_ShouldReturnEmptyCollection_WhenNoFilesMatch()
    {
        CreateFile("a.txt");
        CreateFile("b.txt");

        var aggregator = CreateAggregator();

        aggregator.SortedFiles.Should().BeEmpty();
        aggregator.SortedFilenames.Should().BeEmpty();
    }

    [Test]
    public void GetWrappedRange_ShouldReturnNormalRange_WhenStartBeforeEnd()
    {
        CreateFiles("00", "01", "02", "03");

        var aggregator = CreateAggregator();

        var result = aggregator.GetWrappedRange("01", "03");

        result.Should().Equal(
            "01",
            "02",
            "03");
    }

    [Test]
    public void GetWrappedRange_ShouldReturnWrappedRange_WhenStartAfterEnd()
    {
        CreateFiles("00", "01", "02", "03");

        var aggregator = CreateAggregator();

        var result = aggregator.GetWrappedRange("02", "00");

        result.Should().Equal(
            "02",
            "03",
            "00");
    }

    [Test]
    public void GetWrappedRange_ShouldReturnSingleItem_WhenStartEqualsEnd()
    {
        CreateFiles("00", "01");

        var aggregator = CreateAggregator();

        var result = aggregator.GetWrappedRange("01", "01");

        result.Should().Equal("01");
    }

    [Test]
    public void GetWrappedRange_ShouldReturnFullCycle_WhenSelectingAllWrapped()
    {
        CreateFiles(
            "00", "01", "02", "03",
            "04", "05", "06", "07",
            "08", "09", "10", "11",
            "12", "13", "14", "15");

        var aggregator = CreateAggregator();

        var result = aggregator.GetWrappedRange("08", "07");

        result.Should().Equal(
            "08", "09", "10", "11",
            "12", "13", "14", "15",
            "00", "01", "02", "03",
            "04", "05", "06", "07");
    }

    [Test]
    public void GetWrappedRange_ShouldThrow_WhenStartFileDoesNotExist()
    {
        CreateFile("00");

        var aggregator = CreateAggregator();

        var act = () => aggregator.GetWrappedRange("99", "00");

        act.Should()
            .Throw<ArgumentException>();
    }

    [Test]
    public void GetWrappedRange_ShouldThrow_WhenEndFileDoesNotExist()
    {
        CreateFile("00");

        var aggregator = CreateAggregator();

        var act = () => aggregator.GetWrappedRange("00", "99");

        act.Should()
            .Throw<ArgumentException>();
    }

    [Test]
    public void GetWrappedRange_ShouldThrow_WhenCollectionIsEmpty()
    {
        var aggregator = CreateAggregator();

        var act = () => aggregator.GetWrappedRange("00", "01");

        act.Should()
            .Throw<ArgumentException>();
    }

    [Test]
    public void SortedFiles_ShouldContainFullPaths()
    {
        CreateFile("00");

        var aggregator = CreateAggregator();

        aggregator.SortedFiles.Single()
            .Should()
            .Be(Path.GetFullPath(Path.Combine(_tempDirectory, "00")));
    }

    private LogFilesAggregator CreateAggregator()
    {
        return new LogFilesAggregator(
            _tempDirectory,
            Path.GetFileNameWithoutExtension,
            _ivlLogFilenameTemplate);
    }

    private void CreateFiles(params string[] names)
    {
        foreach (var name in names)
        {
            CreateFile($"{name}");
        }
    }

    private void CreateFile(string filename)
    {
        File.WriteAllText(
            Path.Combine(_tempDirectory, filename),
            string.Empty);
    }
}