using Vyre.App.Models;

namespace Vyre.App.Services.Wifi;

public interface IScanSessionStore
{
    Task SetLatestAsync(ScanInsightSnapshot snapshot, CancellationToken cancellationToken);
    Task<ScanInsightSnapshot?> GetLatestAsync(CancellationToken cancellationToken);
}