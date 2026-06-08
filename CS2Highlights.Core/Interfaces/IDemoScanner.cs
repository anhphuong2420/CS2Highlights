using CS2Highlights.Core.Models;

namespace CS2Highlights.Core.Interfaces;

public interface IDemoScanner
{
    IReadOnlyList<DemoFileInfo> ScanFolder(string folderPath);
}
