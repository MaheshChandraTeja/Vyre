namespace Vyre.App.Models;

public sealed class SavedReportRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string SourcePlatform { get; init; } = string.Empty;
    public string CapabilityMessage { get; init; } = string.Empty;
    public bool IsPartial { get; init; }
    public string JsonPath { get; init; } = string.Empty;
    public string CsvPath { get; init; } = string.Empty;
    public string HtmlPath { get; init; } = string.Empty;
    public int AccessPointCount { get; init; }
    public int IssueCount { get; init; }
}

public sealed class AccessPointComparisonDelta
{
    public string ChangeType { get; init; } = string.Empty;
    public string Bssid { get; init; } = string.Empty;
    public string Ssid { get; init; } = string.Empty;
    public string BeforeSecurity { get; init; } = string.Empty;
    public string AfterSecurity { get; init; } = string.Empty;
    public int BeforeChannel { get; init; }
    public int AfterChannel { get; init; }
    public int BeforeSignalDbm { get; init; }
    public int AfterSignalDbm { get; init; }
    public int SignalDeltaDbm { get; init; }
}

public sealed class CompareReportModel
{
    public string LeftReportId { get; init; } = string.Empty;
    public string RightReportId { get; init; } = string.Empty;
    public IReadOnlyList<AccessPointComparisonDelta> Deltas { get; init; } = Array.Empty<AccessPointComparisonDelta>();
}