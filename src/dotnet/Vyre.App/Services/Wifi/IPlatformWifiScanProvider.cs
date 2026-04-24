using Vyre.App.Models;

namespace Vyre.App.Services.Wifi;

public interface IPlatformWifiScanProvider
{
    Task<PlatformWifiScanPayload> ScanAsync(CancellationToken cancellationToken);
    Task<string> GetCapabilitySummaryAsync(CancellationToken cancellationToken);
}