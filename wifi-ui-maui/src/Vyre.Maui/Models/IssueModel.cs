namespace Vyre.Maui.Models;

public enum IssueSeverity { Info, Warning, Critical }

public sealed record IssueModel(
    string Id,
    IssueSeverity Severity,
    string Message
);
