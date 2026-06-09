using CS2Highlights.Core.Models;

namespace CS2Highlights.Core.Interfaces;

public interface ILightweightDemoReader
{
    Task<DemoHeaderInfo> ReadHeaderAsync(string demoPath, CancellationToken cancellationToken = default);
}
