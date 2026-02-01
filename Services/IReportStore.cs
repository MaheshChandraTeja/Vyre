using Vyre.Maui.Models;

namespace Vyre.Maui.Services;

public interface IReportStore
{
    Task<IReadOnlyList<ReportSummaryModel>> LoadAsync(CancellationToken ct);
    Task AddAsync(ReportSummaryModel report, CancellationToken ct);
    Task ClearAsync(CancellationToken ct);
}
