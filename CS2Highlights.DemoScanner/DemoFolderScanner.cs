using CS2Highlights.Core.Interfaces;
using CS2Highlights.Core.Models;

namespace CS2Highlights.DemoScanner;

public class DemoFolderScanner : IDemoScanner
{
    public IReadOnlyList<DemoFileInfo> ScanFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return Array.Empty<DemoFileInfo>();

        return Directory
            .GetFiles(folderPath, "*.dem", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .Select(fi => new DemoFileInfo
            {
                FilePath = fi.FullName,
                FileName = fi.Name,
                FileSizeBytes = fi.Length,
                LastModified = fi.LastWriteTime
            })
            .OrderByDescending(f => f.LastModified)
            .ToList();
    }
}
