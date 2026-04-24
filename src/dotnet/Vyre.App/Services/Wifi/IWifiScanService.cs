using Vyre.App.Models;

namespace Vyre.App.Services.Wifi;

public interface IWifiScanService
{
    Task<ScanInsightSnapshot> ScanAndAnalyzeAsync(CancellationToken cancellationToken);
    Task<ScanInsightSnapshot?> GetLatestAsync(CancellationToken cancellationToken);
}