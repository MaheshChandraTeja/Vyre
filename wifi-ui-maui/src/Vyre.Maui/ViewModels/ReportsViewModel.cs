using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyre.Maui.Models;
using Vyre.Maui.Services;

namespace Vyre.Maui.ViewModels;

public partial class ReportsViewModel : BaseViewModel
{
    private readonly IReportStore _store;

    public ObservableCollection<ReportSummaryModel> Reports { get; } = new();

    [ObservableProperty] private ReportSummaryModel? selected;

    public ReportsViewModel(IReportStore store)
    {
        _store = store;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var items = await _store.LoadAsync(CancellationToken.None);
            Reports.Clear();
            foreach (var r in items.OrderByDescending(x => x.GeneratedAtUtc))
                Reports.Add(r);

            StatusMessage = $"Reports: {Reports.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ClearAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _store.ClearAsync(CancellationToken.None);
            Reports.Clear();
            Selected = null;
            StatusMessage = "Cleared";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to clear: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ExportSelectedAsync()
    {
        if (Selected is null) { StatusMessage = "Select a report"; return; }

        try
        {
            var fileName = $"vyre-report-{Selected.ReportId}.json";
            var path = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(path, Selected.ReportJson);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export Report",
                File = new ShareFile(path)
            });

            StatusMessage = "Shared";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }
}
