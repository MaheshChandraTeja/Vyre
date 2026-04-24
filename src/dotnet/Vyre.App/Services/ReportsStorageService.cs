using System.Text.Json;
using Vyre.App.Models;

namespace Vyre.App.Services;

public sealed class ReportsStorageService : IReportsStorageService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _rootPath;
    private readonly string _indexPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ReportsStorageService()
    {
        _rootPath = Path.Combine(FileSystem.Current.AppDataDirectory, "reports");
        _indexPath = Path.Combine(_rootPath, "history.json");
    }

    public async Task<IReadOnlyList<ReportSummary>> GetHistoryAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(_rootPath);

            if (!File.Exists(_indexPath))
            {
                var seed = await SeedHistoryAsync(cancellationToken);
                return seed;
            }

            await using var stream = File.OpenRead(_indexPath);
            var items = await JsonSerializer.DeserializeAsync<List<ReportSummary>>(stream, JsonOptions, cancellationToken);
            return (items ?? new List<ReportSummary>())
                .OrderByDescending(x => x.CreatedUtc)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ReportSummary> AddDummyReportAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(_rootPath);

            var current = File.Exists(_indexPath)
                ? await ReadHistoryInternalAsync(cancellationToken)
                : new List<ReportSummary>();

            var reportId = Guid.NewGuid().ToString("N");
            var jsonPath = Path.Combine(_rootPath, $"{reportId}.json");

            var payload = new
            {
                schema = "vyre.report.v1",
                reportId,
                createdUtc = DateTimeOffset.UtcNow,
                summary = new
                {
                    networks = 6,
                    issues = 4
                },
                issues = new[]
                {
                    new { code = "OPEN_NET", severity = "High", title = "Open network detected" },
                    new { code = "WEAK_SEC", severity = "High", title = "WEP network detected" }
                }
            };

            var json = JsonSerializer.Serialize(payload, JsonOptions);
            await File.WriteAllTextAsync(jsonPath, json, cancellationToken);

            var summary = new ReportSummary
            {
                Id = reportId,
                CreatedUtc = DateTimeOffset.UtcNow,
                Title = $"Scan Report {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}",
                NetworkCount = 6,
                IssueCount = 4,
                JsonPath = jsonPath
            };

            current.Insert(0, summary);
            await WriteHistoryInternalAsync(current, cancellationToken);
            return summary;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> ReadReportJsonAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return "{}";
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    private async Task<List<ReportSummary>> SeedHistoryAsync(CancellationToken cancellationToken)
    {
        var seeded = new List<ReportSummary>();

        for (var i = 0; i < 3; i++)
        {
            var id = Guid.NewGuid().ToString("N");
            var created = DateTimeOffset.UtcNow.AddMinutes(-(i + 1) * 25);
            var jsonPath = Path.Combine(_rootPath, $"{id}.json");

            var payload = new
            {
                schema = "vyre.report.v1",
                reportId = id,
                createdUtc = created,
                summary = new
                {
                    networks = 5 + i,
                    issues = 2 + i
                }
            };

            await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(payload, JsonOptions), cancellationToken);

            seeded.Add(new ReportSummary
            {
                Id = id,
                CreatedUtc = created,
                Title = $"Historical Report {i + 1}",
                NetworkCount = 5 + i,
                IssueCount = 2 + i,
                JsonPath = jsonPath
            });
        }

        await WriteHistoryInternalAsync(seeded, cancellationToken);
        return seeded.OrderByDescending(x => x.CreatedUtc).ToList();
    }

    private async Task<List<ReportSummary>> ReadHistoryInternalAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(_indexPath);
        var items = await JsonSerializer.DeserializeAsync<List<ReportSummary>>(stream, JsonOptions, cancellationToken);
        return items ?? new List<ReportSummary>();
    }

    private async Task WriteHistoryInternalAsync(List<ReportSummary> history, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_indexPath);
        await JsonSerializer.SerializeAsync(stream, history, JsonOptions, cancellationToken);
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
