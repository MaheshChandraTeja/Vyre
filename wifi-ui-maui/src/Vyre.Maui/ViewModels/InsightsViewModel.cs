using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyre.Maui.Models;
using Vyre.Maui.Services;

namespace Vyre.Maui.ViewModels;

public partial class InsightsViewModel : BaseViewModel
{
    private readonly IWifiEngine _engine;
    private readonly ISettingsStore _settingsStore;
    private readonly IReportStore _reportStore;

    public ObservableCollection<IssueModel> Issues { get; } = new();
    public ObservableCollection<RecommendationModel> Recommendations { get; } = new();

    [ObservableProperty] private int apCount;

    public InsightsViewModel(IWifiEngine engine, ISettingsStore settingsStore, IReportStore reportStore)
    {
        _engine = engine;
        _settingsStore = settingsStore;
        _reportStore = reportStore;
    }

    [RelayCommand]
    public async Task AnalyzeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Analyzing…";

        try
        {
            var settings = await _settingsStore.LoadAsync(CancellationToken.None);
            var aps = await _engine.ScanAsync(CancellationToken.None);
            ApCount = aps.Count;

            var (issues, recs, reportJson) = await _engine.AnalyzeAsync(aps, settings, CancellationToken.None);

            Issues.Clear();
            foreach (var i in issues.OrderBy(x => x.Id))
                Issues.Add(i);

            Recommendations.Clear();
            foreach (var r in recs.OrderBy(x => x.Id))
                Recommendations.Add(r);

            var summary = new ReportSummaryModel(
                ReportId: $"local-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                AccessPointCount: aps.Count,
                IssueCount: issues.Count,
                RecommendationCount: recs.Count,
                ReportJson: reportJson
            );

            await _reportStore.AddAsync(summary, CancellationToken.None);

            StatusMessage = "Analysis complete";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Analysis failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
