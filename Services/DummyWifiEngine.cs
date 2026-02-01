using System.Text.Json;
using Vyre.Maui.Models;

namespace Vyre.Maui.Services;

public sealed class DummyWifiEngine : IWifiEngine
{
    public async Task<IReadOnlyList<AccessPointModel>> ScanAsync(CancellationToken ct)
    {
        await Task.Delay(250, ct);
        return new List<AccessPointModel>
        {
            new("11:11:11:11:11:11", "HomeWiFi", "2.4ghz", 6, -43, "WPA2"),
            new("22:22:22:22:22:22", "CafeNet", "5ghz", 36, -61, "WPA2"),
            new("33:33:33:33:33:33", "", "5ghz", 149, -70, "WPA3"),
            new("44:44:44:44:44:44", "OfficeMesh", "6ghz", 5, -55, "WPA3"),
        };
    }

    public async Task<(IReadOnlyList<IssueModel>, IReadOnlyList<RecommendationModel>, string)> AnalyzeAsync(
        IReadOnlyList<AccessPointModel> aps,
        AppSettings settings,
        CancellationToken ct)
    {
        await Task.Delay(200, ct);

        var issues = new List<IssueModel>();
        var recs = new List<RecommendationModel>();

        if (aps.Any(a => a.RssiDbm is <= -60))
        {
            issues.Add(new IssueModel("weak_signal", IssueSeverity.Warning,
                "Signal is weak on at least one access point."));
            recs.Add(new RecommendationModel("move_router", RecommendationPriority.High,
                "Move router to a more central location."));
        }

        if (aps.Count(a => a.Band == "2.4ghz") >= 2)
        {
            issues.Add(new IssueModel("likely_congestion", IssueSeverity.Info,
                "2.4GHz band has multiple networks; congestion is likely."));
            recs.Add(new RecommendationModel("switch_to_5ghz", RecommendationPriority.Medium,
                "Prefer 5GHz/6GHz when possible."));
        }

        issues.Add(new IssueModel("report_generated", IssueSeverity.Info,
            "Report generated successfully."));

        var payload = new
        {
            schema_version = 1,
            report_id = $"m3-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            generated_at_utc = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            platform = DeviceInfo.Platform.ToString().ToLowerInvariant(),
            app_version = AppInfo.VersionString,
            access_points = aps,
            issues,
            recommendations = recs
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        return (issues, recs, json);
    }

    public Task<string> GetNativeVersionAsync(CancellationToken ct)
    {
        return Task.FromResult("dummy-native-0.0");
    }
}
