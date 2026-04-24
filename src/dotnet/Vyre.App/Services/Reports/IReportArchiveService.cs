using Vyre.App.Models;

namespace Vyre.App.Services.Reports;

public interface IReportArchiveService
{
    Task<SavedReportRecord> SaveLatestReportAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SavedReportRecord>> ListAsync(CancellationToken cancellationToken);
    Task<CompareReportModel> CompareAsync(string leftReportId, string rightReportId, CancellationToken cancellationToken);
    Task<string> ExportBundleAsync(string reportId, CancellationToken cancellationToken);
}