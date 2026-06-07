using CS2Highlights.Core.Models;

namespace CS2Highlights.Core.Interfaces;

public interface IClipRenderer
{
    Task<string> RenderAsync(RenderJob job, IProgress<RenderProgress>? progress = null);
}
