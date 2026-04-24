using Vyre.App.Models;

namespace Vyre.App.Services.Analysis;

public sealed class ScanAnalysisPipeline : IScanAnalysisPipeline
{
    private readonly IWifiNormalizationService _normalizationService;
    private readonly IWifiAnalyzerService _wifiAnalyzerService;
    private readonly IAnalysisReportStore _analysisReportStore;

    public ScanAnalysisPipeline(
        IWifiNormalizationService normalizationService,
        IWifiAnalyzerService wifiAnalyzerService,
        IAnalysisReportStore analysisReportStore)
    {
        _normalizationService = normalizationService;
        _wifiAnalyzerService = wifiAnalyzerService;
        _analysisReportStore = analysisReportStore;
    }

    public async Task<AnalysisReportModel> AnalyzeAndStoreAsync(
        IReadOnlyList<AccessPointViewData> accessPoints,
        string sourcePlatform,
        bool isPartial,
        string capabilityMessage,
        IReadOnlyList<string>? warnings,
        CancellationToken cancellationToken)
    {
        var normalized = await _normalizationService.NormalizeAsync(
            accessPoints,
            sourcePlatform,
            isPartial,
            cancellationToken);

        var report = await _wifiAnalyzerService.AnalyzeAsync(
            normalized,
            sourcePlatform,
            isPartial,
            capabilityMessage,
            warnings,
            cancellationToken);

        await _analysisReportStore.SetLatestAsync(report, cancellationToken);
        return report;
    }
}