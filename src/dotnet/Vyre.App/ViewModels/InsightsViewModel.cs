using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyre.App.Models;
using Vyre.App.Services.Wifi;

namespace Vyre.App.ViewModels;

public sealed partial class InsightsViewModel : BaseViewModel
{
    private readonly IWifiScanService _wifiScanService;

    [ObservableProperty]
    private string summaryMessage = string.Empty;

    public ObservableCollection<InsightIssue> Issues { get; } = new();
    public ObservableCollection<InsightRecommendation> Recommendations { get; } = new();

    public IAsyncRelayCommand RefreshCommand { get; }

    public InsightsViewModel(IWifiScanService wifiScanService)
    {
        _wifiScanService = wifiScanService;
        Title = "Insights";
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task InitializeAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            using var cts = new CancellationTokenSource();
            var snapshot = await _wifiScanService.GetLatestAsync(cts.Token);

            if (snapshot is null)
            {
                SummaryMessage = "No scan results yet. Run a scan first.";

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Issues.Clear();
                    Recommendations.Clear();
                });

                return;
            }

            var issues = snapshot.Issues?.ToList() ?? new List<InsightIssue>();
            var recommendations = snapshot.Recommendations?.ToList() ?? new List<InsightRecommendation>();

            if (recommendations.Count == 0 && issues.Count > 0)
            {
                recommendations = issues
                    .Select(issue => new InsightRecommendation
                    {
                        Id = string.IsNullOrWhiteSpace(issue.Code) ? Guid.NewGuid().ToString("N") : issue.Code,
                        Priority = issue.Severity switch
                        {
                            "Critical" => "P1",
                            "High" => "P1",
                            "Medium" => "P2",
                            "Low" => "P3",
                            _ => "P3"
                        },
                        Title = string.IsNullOrWhiteSpace(issue.Title) ? "Recommendation" : issue.Title,
                        Description = string.IsNullOrWhiteSpace(issue.FixSteps)
                            ? (string.IsNullOrWhiteSpace(issue.Description)
                                ? "Review the observed network condition and re-run the scan after changes."
                                : issue.Description)
                            : issue.FixSteps
                    })
                    .ToList();
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Issues.Clear();
                foreach (var issue in issues)
                {
                    Issues.Add(issue);
                }

                Recommendations.Clear();
                foreach (var recommendation in recommendations)
                {
                    Recommendations.Add(recommendation);
                }
            });

            var warningSuffix = snapshot.Warnings is { Count: > 0 }
                ? $" • {snapshot.Warnings.Count} warning(s)"
                : string.Empty;

            SummaryMessage =
                $"{snapshot.SourcePlatform} • {snapshot.AccessPoints.Count} AP(s)"
                + (snapshot.IsPartial ? " • limited visibility" : string.Empty)
                + warningSuffix;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load insights: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}