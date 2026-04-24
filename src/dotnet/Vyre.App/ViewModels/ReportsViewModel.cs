using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyre.App.Models;
using Vyre.App.Services.Reports;

namespace Vyre.App.ViewModels;

public sealed partial class ReportsViewModel : BaseViewModel, IDisposable
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    private readonly IReportArchiveService _reportArchiveService;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ObservableCollection<ReportSummary> Reports { get; } = new();

    [ObservableProperty]
    private string selectedReportJson = "{}";

    [ObservableProperty]
    private bool hasReports;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand<ReportSummary> OpenReportCommand { get; }

    public ReportsViewModel(IReportArchiveService reportArchiveService)
    {
        _reportArchiveService = reportArchiveService;
        Title = "Reports";

        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        OpenReportCommand = new AsyncRelayCommand<ReportSummary>(OpenReportAsync);
    }

    public async Task InitializeAsync()
    {
        if (Reports.Count == 0)
        {
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        if (!await _gate.WaitAsync(0))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            using var cts = new CancellationTokenSource();
            var items = (await _reportArchiveService.ListAsync(cts.Token))
                .OrderByDescending(x => x.CapturedAtUtc)
                .Select(ToSummary)
                .ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Reports.Clear();
                foreach (var item in items)
                {
                    Reports.Add(item);
                }

                HasReports = Reports.Count > 0;
            });

            if (items.Count > 0)
            {
                await OpenReportAsync(items[0]);
            }
            else
            {
                SelectedReportJson = "{}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load reports: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _gate.Release();
        }
    }

    private async Task OpenReportAsync(ReportSummary? report)
    {
        if (report is null)
        {
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource();
            SelectedReportJson = await _reportArchiveService.ReadReportJsonAsync(report.Id, cts.Token);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to read report JSON: {ex.Message}";
        }
    }

    private static ReportSummary ToSummary(SavedReportRecord record)
    {
        var platform = string.IsNullOrWhiteSpace(record.SourcePlatform)
            ? "Scan"
            : record.SourcePlatform.Trim();

        return new ReportSummary
        {
            Id = record.Id,
            CreatedUtc = record.CapturedAtUtc,
            Title = string.Create(InvariantCulture, $"{platform} scan"),
            NetworkCount = record.AccessPointCount,
            IssueCount = record.IssueCount,
            JsonPath = record.JsonPath
        };
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
