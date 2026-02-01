using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vyre.Maui.Models;

namespace Vyre.Maui.Services;

public sealed class WindowsWifiEngine : IWifiEngine
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<string> GetNativeVersionAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            return WifiNative.GetVersion();
        }, ct);
    }

    public Task<IReadOnlyList<AccessPointModel>> ScanAsync(CancellationToken ct = default)
    {
        return Task.Run<IReadOnlyList<AccessPointModel>>(() =>
        {
            ct.ThrowIfCancellationRequested();

            var scanJson = WifiNative.ScanJson();
            ct.ThrowIfCancellationRequested();

            return ParseAccessPoints(scanJson);
        }, ct);
    }

    public Task<(IReadOnlyList<IssueModel> Issues, IReadOnlyList<RecommendationModel> Recs, string ReportJson)> AnalyzeAsync(
        IReadOnlyList<AccessPointModel> accessPoints,
        AppSettings settings,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var scanJson = BuildAnalyzeScanJson(accessPoints, settings);

            ct.ThrowIfCancellationRequested();

            var reportJson = WifiNative.AnalyzeJson(scanJson);

            var (issues, recs) = TryParseIssuesAndRecs(reportJson);

            return (issues, recs, reportJson);
        }, ct);
    }

    private static IReadOnlyList<AccessPointModel> ParseAccessPoints(string scanJson)
    {
        if (string.IsNullOrWhiteSpace(scanJson))
            return Array.Empty<AccessPointModel>();

        var trimmed = scanJson.TrimStart();

        try
        {
            // Accept either:
            // 1) [ {ap}, {ap} ]
            // 2) { "access_points": [ {ap}, {ap} ] }

            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                var arr = JsonSerializer.Deserialize<List<AccessPointModel>>(scanJson, JsonOpts);
                return (IReadOnlyList<AccessPointModel>)(arr ?? new List<AccessPointModel>());
            }

            using var doc = JsonDocument.Parse(scanJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("access_points", out var apProp) &&
                apProp.ValueKind == JsonValueKind.Array)
            {
                var arr = JsonSerializer.Deserialize<List<AccessPointModel>>(apProp.GetRawText(), JsonOpts);
                return (IReadOnlyList<AccessPointModel>)(arr ?? new List<AccessPointModel>());
            }
        }
        catch
        {
            // Swallow and return empty. Native can be “creative”.
        }

        return Array.Empty<AccessPointModel>();
    }

    private static string BuildAnalyzeScanJson(IReadOnlyList<AccessPointModel> aps, AppSettings settings)
    {
        var nowUtc = DateTime.UtcNow.ToString("O");

        var payload = new
        {
            report_id = Guid.NewGuid().ToString("D"),
            generated_at_utc = nowUtc,
            captured_at_utc = nowUtc,
            platform = "windows",

            // AppSettings doesn't have AppVersion (per your build error),
            // so keep it deterministic and simple.
            app_version = "0.1.0",

            access_points = BuildAccessPointsArray(aps)
        };

        return JsonSerializer.Serialize(payload);
    }

    private static object[] BuildAccessPointsArray(IReadOnlyList<AccessPointModel> aps)
    {
        var list = new object[aps.Count];
        for (int i = 0; i < aps.Count; i++)
        {
            var ap = aps[i];

            // Your native parse_band expects "2.4ghz" / "5ghz" / "6ghz".
            var band = ap.Band;
            if (string.IsNullOrWhiteSpace(band))
                band = "unknown";

            list[i] = new
            {
                bssid = ap.Bssid ?? "",
                ssid = ap.Ssid ?? "",
                band,
                channel = ap.Channel,
                rssi_dbm = ap.RssiDbm,
                security = ap.Security
            };
        }

        return list;
    }

    private static (IReadOnlyList<IssueModel>, IReadOnlyList<RecommendationModel>) TryParseIssuesAndRecs(string reportJson)
    {
        if (string.IsNullOrWhiteSpace(reportJson))
            return (Array.Empty<IssueModel>(), Array.Empty<RecommendationModel>());

        try
        {
            using var doc = JsonDocument.Parse(reportJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return (Array.Empty<IssueModel>(), Array.Empty<RecommendationModel>());

            IReadOnlyList<IssueModel> issues = Array.Empty<IssueModel>();
            IReadOnlyList<RecommendationModel> recs = Array.Empty<RecommendationModel>();

            if (doc.RootElement.TryGetProperty("issues", out var issuesEl) &&
                issuesEl.ValueKind == JsonValueKind.Array)
            {
                var parsed = JsonSerializer.Deserialize<List<IssueModel>>(issuesEl.GetRawText(), JsonOpts);
                issues = (IReadOnlyList<IssueModel>)(parsed ?? new List<IssueModel>());
            }

            if (doc.RootElement.TryGetProperty("recommendations", out var recsEl) &&
                recsEl.ValueKind == JsonValueKind.Array)
            {
                var parsed = JsonSerializer.Deserialize<List<RecommendationModel>>(recsEl.GetRawText(), JsonOpts);
                recs = (IReadOnlyList<RecommendationModel>)(parsed ?? new List<RecommendationModel>());
            }

            return (issues, recs);
        }
        catch
        {
            return (Array.Empty<IssueModel>(), Array.Empty<RecommendationModel>());
        }
    }
}
