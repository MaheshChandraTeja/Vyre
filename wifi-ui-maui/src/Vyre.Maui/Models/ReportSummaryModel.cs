namespace Vyre.Maui.Models;

public sealed record ReportSummaryModel(
    string ReportId,
    DateTimeOffset GeneratedAtUtc,
    int AccessPointCount,
    int IssueCount,
    int RecommendationCount,
    string ReportJson // stored full JSON for export/share in Milestone 3
);
