namespace Vyre.App.Models;

public sealed class ReportSummary
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public string Title { get; init; } = string.Empty;
    public int NetworkCount { get; init; }
    public int IssueCount { get; init; }
    public string JsonPath { get; init; } = string.Empty;
}