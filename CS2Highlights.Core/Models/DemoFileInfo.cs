namespace CS2Highlights.Core.Models;

public class DemoFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime LastModified { get; set; }
}
