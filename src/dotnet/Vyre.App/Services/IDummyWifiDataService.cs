using Vyre.App.Models;

namespace Vyre.App.Services;

public interface IDummyWifiDataService
{
    Task<IReadOnlyList<AccessPointViewData>> GetAccessPointsAsync(CancellationToken cancellationToken);
    Task<(IReadOnlyList<InsightIssue> Issues, IReadOnlyList<InsightRecommendation> Recommendations)> GetInsightsAsync(CancellationToken cancellationToken);
}