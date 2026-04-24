namespace Vyre.App.Models;

public sealed class ScanInsightSnapshot
{
    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string SourcePlatform { get; init; } = string.Empty;
    public bool IsPartial { get; init; }
    public string CapabilityMessage { get; init; } = string.Empty;
    public string AnalysisReportJson { get; init; } = "{}";
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<AccessPointViewData> AccessPoints { get; init; } = Array.Empty<AccessPointViewData>();
    public IReadOnlyList<InsightIssue> Issues { get; init; } = Array.Empty<InsightIssue>();
    public IReadOnlyList<InsightRecommendation> Recommendations { get; init; } = Array.Empty<InsightRecommendation>();
}