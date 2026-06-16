using CS2Highlights.Core.Models;

namespace CS2Highlights.Renderer;

public static class FfmpegEncoder
{
    // Builds the FFmpeg argument string embedded in the HLAE .cfg script.
    // {QUOTE} and {AFX_STREAM_PATH} are HLAE template variables — they must be literal in the .cfg file.
    public static string BuildArgs(RenderSettings settings)
    {
        var scale       = settings.OutputResolution.Replace("x", ":");
        var encoderArgs = settings.Encoder switch
        {
            "h264_nvenc" => "-c:v h264_nvenc -preset p4 -b:v 20M -pix_fmt yuv420p",
            "libx264"    => "-c:v libx264 -crf 18 -preset medium -pix_fmt yuv420p",
            _            => $"-c:v {settings.Encoder} -pix_fmt yuv420p"
        };
        return $"{encoderArgs} -vf scale={scale} {{QUOTE}}{{AFX_STREAM_PATH}}\\\\clip.mp4{{QUOTE}}";
    }
}
