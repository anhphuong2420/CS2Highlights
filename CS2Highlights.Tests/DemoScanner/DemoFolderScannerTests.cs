using CS2Highlights.DemoScanner;

namespace CS2Highlights.Tests.DemoScanner;

[TestFixture]
public class DemoFolderScannerTests
{
    private string _tempFolder = string.Empty;
    private DemoFolderScanner _scanner = null!;

    [SetUp]
    public void SetUp()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempFolder);
        _scanner = new DemoFolderScanner();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }

    [Test]
    public void ScanFolder_EmptyFolder_ReturnsEmpty()
    {
        var result = _scanner.ScanFolder(_tempFolder);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ScanFolder_NonExistentFolder_ReturnsEmpty()
    {
        var result = _scanner.ScanFolder(@"C:\this\does\not\exist");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ScanFolder_WithDemFiles_ReturnsThem()
    {
        File.WriteAllText(Path.Combine(_tempFolder, "a.dem"), "fake");
        File.WriteAllText(Path.Combine(_tempFolder, "b.dem"), "fake");
        File.WriteAllText(Path.Combine(_tempFolder, "c.txt"), "ignored");

        var result = _scanner.ScanFolder(_tempFolder);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(f => f.FileName), Is.EquivalentTo(new[] { "a.dem", "b.dem" }));
    }

    [Test]
    public void ScanFolder_PopulatesAllFields()
    {
        var path = Path.Combine(_tempFolder, "test.dem");
        File.WriteAllText(path, "content");

        var result = _scanner.ScanFolder(_tempFolder);

        Assert.That(result, Has.Count.EqualTo(1));
        var info = result[0];
        Assert.That(info.FilePath, Is.EqualTo(path));
        Assert.That(info.FileName, Is.EqualTo("test.dem"));
        Assert.That(info.FileSizeBytes, Is.GreaterThan(0));
        Assert.That(info.LastModified, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void ScanFolder_OrdersByLastModifiedDescending()
    {
        var older = Path.Combine(_tempFolder, "older.dem");
        var newer = Path.Combine(_tempFolder, "newer.dem");

        File.WriteAllText(older, "x");
        File.SetLastWriteTime(older, DateTime.Now.AddMinutes(-10));

        File.WriteAllText(newer, "x");
        File.SetLastWriteTime(newer, DateTime.Now);

        var result = _scanner.ScanFolder(_tempFolder);

        Assert.That(result[0].FileName, Is.EqualTo("newer.dem"));
        Assert.That(result[1].FileName, Is.EqualTo("older.dem"));
    }
}
