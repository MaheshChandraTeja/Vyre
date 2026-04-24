using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyre.App.Models;
using Vyre.App.Services;

namespace Vyre.App.ViewModels;

public sealed partial class ReportsViewModel : BaseViewModel, IDisposable
{
    private readonly IReportsStorageService _reportsStorageService;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ObservableCollection<ReportSummary> Reports { get; } = new();

    [ObservableProperty]
    private string selectedReportJson = "{}";

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand AddDummyReportCommand { get; }
    public IAsyncRelayCommand<ReportSummary> OpenReportCommand { get; }

    public ReportsViewModel(IReportsStorageService reportsStorageService)
    {
        _reportsStorageService = reportsStorageService;
        Title = "Reports";

        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        AddDummyReportCommand = new AsyncRelayCommand(AddDummyReportAsync);
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
            var items = await _reportsStorageService.GetHistoryAsync(cts.Token);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Reports.Clear();
                foreach (var item in items)
                {
                    Reports.Add(item);
                }
            });

            if (Reports.Count > 0)
            {
                await OpenReportAsync(Reports[0]);
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

    private async Task AddDummyReportAsync()
    {
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource();

            var report = await _reportsStorageService.AddDummyReportAsync(cts.Token);

            MainThread.BeginInvokeOnMainThread(() => Reports.Insert(0, report));
            await OpenReportAsync(report);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create report: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
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
            SelectedReportJson = await _reportsStorageService.ReadReportJsonAsync(report.JsonPath, cts.Token);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to read report JSON: {ex.Message}";
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
