namespace Vyre.Maui.Models;

public enum RecommendationPriority { Low, Medium, High }

public sealed record RecommendationModel(
    string Id,
    RecommendationPriority Priority,
    string Message
);
