namespace Vyre.App.Models;

public sealed class InsightIssue
{
    public string Code { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Evidence { get; init; } = string.Empty;
    public string FixSteps { get; init; } = string.Empty;
    public int Rank { get; init; }

    public string SeverityLabel => string.IsNullOrWhiteSpace(Severity) ? "Info" : Severity;
    public string CategoryLabel => string.IsNullOrWhiteSpace(Category) ? "General" : Category;
    public bool HasEvidence => !string.IsNullOrWhiteSpace(Evidence);
    public bool HasFixSteps => !string.IsNullOrWhiteSpace(FixSteps);

    public string SeverityColorHex => SeverityLabel switch
    {
        "Critical" => "#FCA5A5",
        "High" => "#FCA5A5",
        "Medium" => "#FBBF24",
        "Low" => "#93C5FD",
        _ => "#67E8F9"
    };

    public string SeverityBackgroundHex => SeverityLabel switch
    {
        "Critical" => "#7F1D1D",
        "High" => "#7F1D1D",
        "Medium" => "#713F12",
        "Low" => "#172554",
        _ => "#164E63"
    };
}

public sealed class InsightRecommendation
{
    public string Id { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Evidence { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public string Effort { get; init; } = string.Empty;

    public string PriorityLabel => string.IsNullOrWhiteSpace(Priority) ? "P3" : Priority;
    public string CategoryLabel => string.IsNullOrWhiteSpace(Category) ? "Action" : Category;
    public bool HasAction => !string.IsNullOrWhiteSpace(Action);
    public bool HasEvidence => !string.IsNullOrWhiteSpace(Evidence);
    public bool HasImpact => !string.IsNullOrWhiteSpace(Impact);
    public bool HasEffort => !string.IsNullOrWhiteSpace(Effort);

    public string PriorityColorHex => PriorityLabel switch
    {
        "P0" => "#FCA5A5",
        "P1" => "#FCA5A5",
        "P2" => "#FBBF24",
        _ => "#93C5FD"
    };

    public string PriorityBackgroundHex => PriorityLabel switch
    {
        "P0" => "#7F1D1D",
        "P1" => "#7F1D1D",
        "P2" => "#713F12",
        _ => "#172554"
    };
}

public sealed class InsightMetricCard
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string AccentHex { get; init; } = "#38BDF8";
}
