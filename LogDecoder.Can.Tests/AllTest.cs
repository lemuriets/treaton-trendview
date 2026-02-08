using LogDecoder.CAN;

namespace LogDecoder.Can.Tests;

[TestFixture]
public class CanPackageParserTests
{
    [Test]
    public void FromBytes_ShouldParseStandardPackage()
    {
        // Arrange
        byte dataLength = 3;
        byte firstByte = dataLength; // < 0x80 => Standard
        int id = 0x1234;
        byte[] expectedData = { 0xAA, 0xBB, 0xCC };
        int hrc = 0x010203;

        var raw = new byte[]
        {
            firstByte,
            0x34, 0x12,                 // ID (2 bytes little endian)
            0xAA, 0xBB, 0xCC,           // Data
            0x03, 0x02, 0x01            // HRC (3 bytes little endian)
        };

        // Act
        var package = CanPackageParser.FromBytes(raw);

        // Assert
        Assert.That(package.Type, Is.EqualTo(PackageType.Standard));
        Assert.That(package.Id, Is.EqualTo(id));
        Assert.That(package.Data, Is.EqualTo(expectedData));
        Assert.That(package.Hrc, Is.EqualTo(hrc));
        Assert.That(package.Length, Is.EqualTo(1 + 2 + 3 + 3));
    }

    [Test]
    public void FromBytes_ShouldParseExtendedPackage()
    {
        // Arrange
        byte dataLength = 2;
        byte firstByte = (byte)(0x80 | dataLength); // >= 0x80 => Extended
        int id = 0x11223344;
        byte[] expectedData = { 0x10, 0x20 };
        int hrc = 0x000102;

        var raw = new byte[]
        {
            firstByte,
            0x44, 0x33, 0x22, 0x11,     // ID (4 bytes little endian)
            0x10, 0x20,                 // Data
            0x02, 0x01, 0x00            // HRC (3 bytes little endian)
        };

        // Act
        var package = CanPackageParser.FromBytes(raw);

        // Assert
        Assert.That(package.Type, Is.EqualTo(PackageType.Extended));
        Assert.That(package.Id, Is.EqualTo(id));
        Assert.That(package.Data, Is.EqualTo(expectedData));
        Assert.That(package.Hrc, Is.EqualTo(hrc));
        Assert.That(package.Length, Is.EqualTo(1 + 4 + 2 + 3));
    }

    [Test]
    public void FromBytes_ShouldReturnEmptyPackage_WhenDataSizeTooLarge()
    {
        // Arrange
        byte firstByte = 0x09; // data size = 9 (> MaxDataSize = 8)

        var raw = new byte[]
        {
            firstByte,
            0x00, 0x00,
            0x00, 0x00, 0x00
        };

        // Act
        var package = CanPackageParser.FromBytes(raw);

        // Assert
        Assert.That(package, Is.EqualTo(default(CanPackage)));
    }

    [TestCase(0x01, PackageType.Standard)]
    [TestCase(0x7F, PackageType.Standard)]
    [TestCase(0x80, PackageType.Extended)]
    [TestCase(0xFF, PackageType.Extended)]
    public void GetPackageType_ShouldReturnCorrectType(byte firstByte, PackageType expected)
    {
        var type = CanPackageParser.GetPackageType(firstByte);

        Assert.That(type, Is.EqualTo(expected));
    }

    [Test]
    public void GetPackageLength_ShouldCalculateCorrectLength_Standard()
    {
        byte firstByte = 0x03; // data size = 3

        var length = CanPackageParser.GetPackageLength(firstByte, PackageType.Standard);

        Assert.That(length, Is.EqualTo(1 + 2 + 3 + 3));
    }

    [Test]
    public void GetPackageLength_ShouldCalculateCorrectLength_Extended()
    {
        byte firstByte = 0x82; // data size = 2 (0x82 & 0x7F)

        var length = CanPackageParser.GetPackageLength(firstByte, PackageType.Extended);

        Assert.That(length, Is.EqualTo(1 + 4 + 2 + 3));
    }
}
