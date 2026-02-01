using Vyre.Maui.Models;

namespace Vyre.Maui.Services;

public interface IWifiEngine
{
    Task<IReadOnlyList<AccessPointModel>> ScanAsync(CancellationToken ct);

    Task<(IReadOnlyList<IssueModel> Issues,
          IReadOnlyList<RecommendationModel> Recs,
          string ReportJson)> AnalyzeAsync(
        IReadOnlyList<AccessPointModel> aps,
        AppSettings settings,
        CancellationToken ct);

    Task<string> GetNativeVersionAsync(CancellationToken ct);
}
