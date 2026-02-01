using System.Text.Json;
using Vyre.Maui.Models;

namespace Vyre.Maui.Services;

public sealed class FileReportStore : IReportStore
{
    private static readonly string Folder = FileSystem.AppDataDirectory;
    private static readonly string PathFile = System.IO.Path.Combine(Folder, "reports.v1.json");

    private sealed record Persisted(IReadOnlyList<ReportSummaryModel> Reports);

    public async Task<IReadOnlyList<ReportSummaryModel>> LoadAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!File.Exists(PathFile))
            return Array.Empty<ReportSummaryModel>();

        await using var fs = File.OpenRead(PathFile);
        var persisted = await JsonSerializer.DeserializeAsync<Persisted>(fs, cancellationToken: ct);
        return persisted?.Reports ?? Array.Empty<ReportSummaryModel>();
    }

    public async Task AddAsync(ReportSummaryModel report, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existing = (await LoadAsync(ct)).ToList();
        existing.Insert(0, report); // newest first

        // Keep history bounded (no one needs 500 reports, relax).
        if (existing.Count > 50)
            existing = existing.Take(50).ToList();

        Directory.CreateDirectory(Folder);
        await using var fs = File.Create(PathFile);

        await JsonSerializer.SerializeAsync(fs, new Persisted(existing), cancellationToken: ct);
    }

    public async Task ClearAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (File.Exists(PathFile))
            File.Delete(PathFile);

        await Task.CompletedTask;
    }
}
