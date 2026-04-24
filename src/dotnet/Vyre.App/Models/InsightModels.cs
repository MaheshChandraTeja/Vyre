namespace Vyre.App.Models;

public sealed class InsightIssue
{
    public string Code { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Evidence { get; init; } = string.Empty;
    public string FixSteps { get; init; } = string.Empty;
    public int Rank { get; init; }
    }

public sealed class InsightRecommendation
{
    public string Id { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}