namespace Vyre.App.Models;

public sealed class AnalysisReportModel
{
    public string Schema { get; init; } = "vyre.report.v1";
    public string SourcePlatform { get; init; } = string.Empty;
    public bool IsPartial { get; init; }
    public string CapabilityMessage { get; init; } = string.Empty;
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<AccessPointViewData> AccessPoints { get; init; } = Array.Empty<AccessPointViewData>();
    public IReadOnlyList<InsightIssue> Issues { get; init; } = Array.Empty<InsightIssue>();
}