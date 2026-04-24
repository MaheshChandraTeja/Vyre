using System.Globalization;
using System.Text;
using System.Text.Json;
using Vyre.App.Models;
using Vyre.App.Services.Analysis;

namespace Vyre.App.Services.Reports;

public sealed class ReportArchiveService : IReportArchiveService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly IAnalysisReportStore _analysisReportStore;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _rootDir;
    private readonly string _indexPath;

    public ReportArchiveService(IAnalysisReportStore analysisReportStore)
    {
        _analysisReportStore = analysisReportStore;
        _rootDir = Path.Combine(FileSystem.Current.AppDataDirectory, "report-archive");
        _indexPath = Path.Combine(_rootDir, "index.json");
    }

    public async Task<SavedReportRecord> SaveLatestReportAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(_rootDir);

            var latest = await _analysisReportStore.GetLatestAsync(cancellationToken)
                ?? throw new InvalidOperationException("No analyzed report is available to save.");

            var id = Guid.NewGuid().ToString("N");
            var reportDir = Path.Combine(_rootDir, id);
            Directory.CreateDirectory(reportDir);

            var jsonPath = Path.Combine(reportDir, "report.json");
            var csvPath = Path.Combine(reportDir, "report.csv");
            var htmlPath = Path.Combine(reportDir, "report.html");

            await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(latest, JsonOptions), cancellationToken);
            await File.WriteAllTextAsync(csvPath, BuildCsv(latest), cancellationToken);
            await File.WriteAllTextAsync(htmlPath, BuildHtml(latest), cancellationToken);

            var index = await LoadIndexInternalAsync(cancellationToken);
            var saved = new SavedReportRecord
            {
                Id = id,
                CapturedAtUtc = DateTimeOffset.UtcNow,
                SourcePlatform = latest.SourcePlatform,
                CapabilityMessage = latest.CapabilityMessage,
                IsPartial = latest.IsPartial,
                JsonPath = jsonPath,
                CsvPath = csvPath,
                HtmlPath = htmlPath,
                AccessPointCount = latest.AccessPoints.Count,
                IssueCount = latest.Issues.Count
            };

            index.Insert(0, saved);
            await SaveIndexInternalAsync(index, cancellationToken);
            return saved;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<SavedReportRecord>> ListAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadIndexInternalAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CompareReportModel> CompareAsync(string leftReportId, string rightReportId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var index = await LoadIndexInternalAsync(cancellationToken);
            var left = index.FirstOrDefault(x => x.Id == leftReportId)
                ?? throw new InvalidOperationException("Left report not found.");
            var right = index.FirstOrDefault(x => x.Id == rightReportId)
                ?? throw new InvalidOperationException("Right report not found.");

            var leftReport = JsonSerializer.Deserialize<AnalysisReportModel>(await File.ReadAllTextAsync(left.JsonPath, cancellationToken), JsonOptions)
                ?? throw new InvalidOperationException("Failed to load left report.");
            var rightReport = JsonSerializer.Deserialize<AnalysisReportModel>(await File.ReadAllTextAsync(right.JsonPath, cancellationToken), JsonOptions)
                ?? throw new InvalidOperationException("Failed to load right report.");

            var leftMap = leftReport.AccessPoints
                .Where(x => !string.IsNullOrWhiteSpace(x.Bssid))
                .ToDictionary(x => x.Bssid, StringComparer.OrdinalIgnoreCase);

            var rightMap = rightReport.AccessPoints
                .Where(x => !string.IsNullOrWhiteSpace(x.Bssid))
                .ToDictionary(x => x.Bssid, StringComparer.OrdinalIgnoreCase);

            var deltas = new List<AccessPointComparisonDelta>();

            foreach (var pair in leftMap)
            {
                if (!rightMap.TryGetValue(pair.Key, out var newer))
                {
                    deltas.Add(new AccessPointComparisonDelta
                    {
                        ChangeType = "Removed",
                        Bssid = pair.Key,
                        Ssid = pair.Value.Ssid,
                        BeforeSecurity = pair.Value.Security,
                        BeforeChannel = pair.Value.Channel,
                        BeforeSignalDbm = pair.Value.SignalDbm
                    });
                    continue;
                }

                if (!string.Equals(pair.Value.Security, newer.Security, StringComparison.OrdinalIgnoreCase)
                    || pair.Value.Channel != newer.Channel
                    || pair.Value.SignalDbm != newer.SignalDbm)
                {
                    deltas.Add(new AccessPointComparisonDelta
                    {
                        ChangeType = "Changed",
                        Bssid = pair.Key,
                        Ssid = string.IsNullOrWhiteSpace(newer.Ssid) ? pair.Value.Ssid : newer.Ssid,
                        BeforeSecurity = pair.Value.Security,
                        AfterSecurity = newer.Security,
                        BeforeChannel = pair.Value.Channel,
                        AfterChannel = newer.Channel,
                        BeforeSignalDbm = pair.Value.SignalDbm,
                        AfterSignalDbm = newer.SignalDbm,
                        SignalDeltaDbm = newer.SignalDbm - pair.Value.SignalDbm
                    });
                }
            }

            foreach (var pair in rightMap)
            {
                if (!leftMap.ContainsKey(pair.Key))
                {
                    deltas.Add(new AccessPointComparisonDelta
                    {
                        ChangeType = "New",
                        Bssid = pair.Key,
                        Ssid = pair.Value.Ssid,
                        AfterSecurity = pair.Value.Security,
                        AfterChannel = pair.Value.Channel,
                        AfterSignalDbm = pair.Value.SignalDbm
                    });
                }
            }

            deltas = deltas
                .OrderBy(x => x.ChangeType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Bssid, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new CompareReportModel
            {
                LeftReportId = leftReportId,
                RightReportId = rightReportId,
                Deltas = deltas
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> ExportBundleAsync(string reportId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var index = await LoadIndexInternalAsync(cancellationToken);
            var record = index.FirstOrDefault(x => x.Id == reportId)
                ?? throw new InvalidOperationException("Report not found.");

            var bundleDir = Path.Combine(_rootDir, record.Id);
            if (!Directory.Exists(bundleDir))
            {
                throw new InvalidOperationException("Report bundle folder is missing.");
            }

            return bundleDir;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<SavedReportRecord>> LoadIndexInternalAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_indexPath))
        {
            return new List<SavedReportRecord>();
        }

        await using var stream = File.OpenRead(_indexPath);
        var items = await JsonSerializer.DeserializeAsync<List<SavedReportRecord>>(stream, JsonOptions, cancellationToken);
        return items ?? new List<SavedReportRecord>();
    }

    private async Task SaveIndexInternalAsync(List<SavedReportRecord> index, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_indexPath);
        await JsonSerializer.SerializeAsync(stream, index, JsonOptions, cancellationToken);
    }

    private static string BuildCsv(AnalysisReportModel report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SSID,BSSID,Vendor,Band,Security,Channel,FrequencyMhz,SignalDbm,ConfidenceScore");

        foreach (var ap in report.AccessPoints)
        {
            sb.Append('"').Append(ap.Ssid.Replace("\"", "\"\"")).Append("\",")
              .Append('"').Append(ap.Bssid.Replace("\"", "\"\"")).Append("\",")
              .Append('"').Append(ap.Vendor.Replace("\"", "\"\"")).Append("\",")
              .Append('"').Append(ap.Band.Replace("\"", "\"\"")).Append("\",")
              .Append('"').Append(ap.Security.Replace("\"", "\"\"")).Append("\",")
              .Append(ap.Channel).Append(',')
              .Append(ap.FrequencyMhz).Append(',')
              .Append(ap.SignalDbm).Append(',')
              .Append(ap.ConfidenceScore)
              .AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildHtml(AnalysisReportModel report)
    {
        static string H(string? s) => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);

        var sb = new StringBuilder();
        sb.Append("""
<!doctype html>
<html><head><meta charset="utf-8"><title>Vyre Report</title>
<style>
body{font-family:Inter,Segoe UI,Arial,sans-serif;background:#0b1020;color:#e5e7eb;margin:0;padding:24px}
.card{background:#111827;border:1px solid #1f2937;border-radius:16px;padding:16px;margin-bottom:16px}
table{width:100%;border-collapse:collapse;font-size:14px}
th,td{padding:10px;border-bottom:1px solid #1f2937;text-align:left;vertical-align:top}
th{color:#93c5fd;font-weight:600}
.muted{color:#94a3b8}
</style></head><body>
""");

        sb.Append(CultureInfo.InvariantCulture, $"""
<div class="card">
<h1>Vyre Scan Report</h1>
<div class="muted">Source Platform: {H(report.SourcePlatform)}</div>
<div class="muted">Capability: {H(report.CapabilityMessage)}</div>
</div>
<div class="card"><h2>Access Points</h2><table><thead><tr>
<th>SSID</th><th>BSSID</th><th>Vendor</th><th>Band</th><th>Security</th><th>Channel</th><th>Signal</th><th>Confidence</th>
</tr></thead><tbody>
""");

        foreach (var ap in report.AccessPoints)
        {
            sb.Append(CultureInfo.InvariantCulture, $"""
<tr>
<td>{H(ap.Ssid)}</td>
<td>{H(ap.Bssid)}</td>
<td>{H(ap.Vendor)}</td>
<td>{H(ap.Band)}</td>
<td>{H(ap.Security)}</td>
<td>{ap.Channel}</td>
<td>{ap.SignalDbm} dBm</td>
<td>{ap.ConfidenceScore:F2}</td>
</tr>
""");
        }

        sb.Append("</tbody></table></div><div class=\"card\"><h2>Insights</h2><table><thead><tr><th>Rank</th><th>Severity</th><th>Title</th><th>Description</th><th>Evidence</th><th>Fix Steps</th></tr></thead><tbody>");

        foreach (var issue in report.Issues)
        {
            sb.Append(CultureInfo.InvariantCulture, $"""
<tr>
<td>{issue.Rank}</td>
<td>{H(issue.Severity)}</td>
<td>{H(issue.Title)}</td>
<td>{H(issue.Description)}</td>
<td>{H(issue.Evidence)}</td>
<td>{H(issue.FixSteps)}</td>
</tr>
""");
        }

        sb.Append("</tbody></table></div></body></html>");
        return sb.ToString();
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
