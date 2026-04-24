using Vyre.App.Models;

namespace Vyre.App.Services.Analysis;

public interface IAnalysisReportStore
{
    Task SetLatestAsync(AnalysisReportModel report, CancellationToken cancellationToken);
    Task<AnalysisReportModel?> GetLatestAsync(CancellationToken cancellationToken);
}