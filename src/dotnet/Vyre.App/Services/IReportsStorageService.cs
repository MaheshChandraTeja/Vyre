using Vyre.App.Models;

namespace Vyre.App.Services;

public interface IReportsStorageService
{
    Task<IReadOnlyList<ReportSummary>> GetHistoryAsync(CancellationToken cancellationToken);
    Task<ReportSummary> AddDummyReportAsync(CancellationToken cancellationToken);
    Task<string> ReadReportJsonAsync(string path, CancellationToken cancellationToken);
}