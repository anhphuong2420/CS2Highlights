namespace CS2Highlights.Core.Models;

public class RenderSettings
{
    public int BufferBeforeSeconds { get; set; } = 5;
    public int BufferAfterSeconds { get; set; } = 3;
    public string OutputResolution { get; set; } = "1920x1080";
    public int OutputFps { get; set; } = 60;
    public string Encoder { get; set; } = "h264_nvenc";
}
