using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vyre.App.Models;
using Vyre.App.Services.Engine;

namespace Vyre.App.Services.Wifi;

public sealed partial class WifiScanService : IWifiScanService
{
    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Native analysis failed. Falling back to managed local rules.")]
    private static partial void LogNativeAnalysisFallback(ILogger logger, Exception exception);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IPlatformWifiScanProvider _platformWifiScanProvider;
    private readonly IVyreEngineService _engineService;
    private readonly IScanSessionStore _scanSessionStore;
    private readonly ILogger<WifiScanService> _logger;

    public WifiScanService(
        IPlatformWifiScanProvider platformWifiScanProvider,
        IVyreEngineService engineService,
        IScanSessionStore scanSessionStore,
        ILogger<WifiScanService> logger)
    {
        _platformWifiScanProvider = platformWifiScanProvider;
        _engineService = engineService;
        _scanSessionStore = scanSessionStore;
        _logger = logger;
    }

    public async Task<ScanInsightSnapshot> ScanAndAnalyzeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = await _platformWifiScanProvider.ScanAsync(cancellationToken);
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        string reportJson;
        try
        {
            reportJson = await _engineService.SubmitScanResultsJsonAsync(payloadJson, cancellationToken);
        }
        catch (Exception ex)
        {
            LogNativeAnalysisFallback(_logger, ex);
            reportJson = "{}";
        }

        var snapshot = BuildSnapshot(payload, reportJson);
        await _scanSessionStore.SetLatestAsync(snapshot, cancellationToken);
        return snapshot;
    }

    public Task<ScanInsightSnapshot?> GetLatestAsync(CancellationToken cancellationToken) =>
        _scanSessionStore.GetLatestAsync(cancellationToken);

    private static ScanInsightSnapshot BuildSnapshot(PlatformWifiScanPayload payload, string reportJson)
    {
        var issues = new List<InsightIssue>();
        var recommendations = new List<InsightRecommendation>();

        TryPopulateFromReportJson(reportJson, issues, recommendations);

        if (issues.Count == 0 && recommendations.Count == 0)
        {
            ApplyManagedFallbackRules(payload, issues, recommendations);
        }

        return new ScanInsightSnapshot
        {
            CapturedAtUtc = payload.CapturedAtUtc,
            SourcePlatform = payload.SourcePlatform,
            IsPartial = payload.IsPartial,
            CapabilityMessage = payload.CapabilityMessage,
            AnalysisReportJson = string.IsNullOrWhiteSpace(reportJson) ? "{}" : reportJson,
            Warnings = payload.Warnings,
            AccessPoints = payload.AccessPoints,
            Issues = issues,
            Recommendations = recommendations
        };
    }

    private static void TryPopulateFromReportJson(
        string reportJson,
        List<InsightIssue> issues,
        List<InsightRecommendation> recommendations)
    {
        if (string.IsNullOrWhiteSpace(reportJson) || reportJson == "{}")
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(reportJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("issues", out var issuesElement) && issuesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in issuesElement.EnumerateArray())
                {
                    issues.Add(new InsightIssue
                    {
                        Code = item.TryGetProperty("code", out var code) ? code.GetString() ?? string.Empty : string.Empty,
                        Severity = item.TryGetProperty("severity", out var severity) ? severity.GetString() ?? "Info" : "Info",
                        Title = item.TryGetProperty("title", out var title) ? title.GetString() ?? "Issue" : "Issue",
                        Description = item.TryGetProperty("description", out var description) ? description.GetString() ?? string.Empty : string.Empty
                    });
                }
            }

            if (root.TryGetProperty("recommendations", out var recsElement) && recsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in recsElement.EnumerateArray())
                {
                    recommendations.Add(new InsightRecommendation
                    {
                        Id = item.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N"),
                        Priority = item.TryGetProperty("priority", out var priority) ? priority.GetString() ?? "P3" : "P3",
                        Title = item.TryGetProperty("title", out var title) ? title.GetString() ?? "Recommendation" : "Recommendation",
                        Description = item.TryGetProperty("description", out var description) ? description.GetString() ?? string.Empty : string.Empty
                    });
                }
            }
        }
        catch
        {
            // Fall back to managed rules. No drama.
        }
    }

    private static void ApplyManagedFallbackRules(
        PlatformWifiScanPayload payload,
        List<InsightIssue> issues,
        List<InsightRecommendation> recommendations)
    {
        var aps = payload.AccessPoints;

        if (payload.IsPartial)
        {
            issues.Add(new InsightIssue
            {
                Code = "PLATFORM_LIMITATION",
                Severity = "Info",
                Title = "Platform-limited scan",
                Description = payload.CapabilityMessage
            });

            recommendations.Add(new InsightRecommendation
            {
                Id = "REC-IOS-001",
                Priority = "P3",
                Title = "Use diagnostics instead of nearby AP enumeration",
                Description = "This platform does not expose full nearby Wi-Fi scanning to standard apps. Rely on reachability, DNS, and latency diagnostics."
            });
        }

        if (aps.Any(x => x.IsOpen))
        {
            issues.Add(new InsightIssue
            {
                Code = "OPEN_NET",
                Severity = "High",
                Title = "Open network detected",
                Description = "At least one visible network is not protected by encryption."
            });

            recommendations.Add(new InsightRecommendation
            {
                Id = "REC-OPEN-001",
                Priority = "P1",
                Title = "Avoid open networks",
                Description = "Prefer WPA2 or WPA3 secured networks. Open SSIDs are not suitable for sensitive traffic."
            });
        }

        if (aps.Any(x => x.Security.Contains("WEP", StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add(new InsightIssue
            {
                Code = "LEGACY_WEP",
                Severity = "High",
                Title = "Weak legacy security detected",
                Description = "A WEP-protected access point was observed. WEP is obsolete and insecure."
            });

            recommendations.Add(new InsightRecommendation
            {
                Id = "REC-WEP-001",
                Priority = "P1",
                Title = "Replace legacy security",
                Description = "Retire WEP and move to WPA2 or WPA3."
            });
        }

        if (aps.Any(x => x.SignalDbm <= -75))
        {
            issues.Add(new InsightIssue
            {
                Code = "WEAK_SIGNAL",
                Severity = "Medium",
                Title = "Weak signal detected",
                Description = "One or more networks have low RSSI and may perform poorly."
            });

            recommendations.Add(new InsightRecommendation
            {
                Id = "REC-SIGNAL-001",
                Priority = "P2",
                Title = "Improve signal quality",
                Description = "Move closer to the access point or improve placement and coverage."
            });
        }

        foreach (var warning in payload.Warnings)
        {
            issues.Add(new InsightIssue
            {
                Code = "PLATFORM_WARNING",
                Severity = "Info",
                Title = "Platform warning",
                Description = warning
            });
        }

        if (issues.Count == 0)
        {
            issues.Add(new InsightIssue
            {
                Code = "NO_MAJOR_ISSUES",
                Severity = "Info",
                Title = "No major issues detected",
                Description = "No obvious security or signal problems were detected from the available scan data."
            });
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new InsightRecommendation
            {
                Id = "REC-GENERAL-001",
                Priority = "P3",
                Title = "Re-run diagnostics periodically",
                Description = "Repeat scans and diagnostics under real network conditions to catch intermittent issues."
            });
        }
    }
}
