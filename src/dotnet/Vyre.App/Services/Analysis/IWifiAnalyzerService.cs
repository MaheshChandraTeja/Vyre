using Vyre.App.Models;

namespace Vyre.App.Services.Analysis;

public interface IWifiAnalyzerService
{
    Task<AnalysisReportModel> AnalyzeAsync(
        IReadOnlyList<AccessPointViewData> accessPoints,
        string sourcePlatform,
        bool isPartial,
        string capabilityMessage,
        IReadOnlyList<string>? warnings,
        CancellationToken cancellationToken);
}