using Vyre.App.Models;

namespace Vyre.App.Services.Analysis;

public interface IScanAnalysisPipeline
{
    Task<AnalysisReportModel> AnalyzeAndStoreAsync(
        IReadOnlyList<AccessPointViewData> accessPoints,
        string sourcePlatform,
        bool isPartial,
        string capabilityMessage,
        IReadOnlyList<string>? warnings,
        CancellationToken cancellationToken);
}