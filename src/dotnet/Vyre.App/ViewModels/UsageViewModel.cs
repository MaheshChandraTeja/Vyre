using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyre.App.Models;
using Vyre.App.Services.Usage;

namespace Vyre.App.ViewModels;

public sealed partial class UsageViewModel : BaseViewModel, IDisposable
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    private readonly IAppNetworkUsageService _usageService;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    private List<AppNetworkUsageModel> _allItems = new();

    public ObservableCollection<UsageAppRowViewModel> UsageItems { get; } = new();

    public IReadOnlyList<string> NetworkScopeOptions { get; } = ["All", "Wi-Fi", "Mobile"];
    public IReadOnlyList<string> TimeRangeOptions { get; } = ["24h", "7d", "30d"];
    public IReadOnlyList<string> SortOptions { get; } = ["Total", "Upload", "Download", "App name"];
    public IReadOnlyList<string> ActivityFilterOptions { get; } = ["All apps", "Active only", "100 KB+", "1 MB+", "10 MB+"];

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedNetworkScope = "All";
    [ObservableProperty] private string selectedTimeRange = "24h";
    [ObservableProperty] private string selectedSort = "Total";
    [ObservableProperty] private string selectedActivityFilter = "All apps";

    [ObservableProperty] private string currentNetworkType = "Unknown";
    [ObservableProperty] private string totalUploadText = "0 B";
    [ObservableProperty] private string totalDownloadText = "0 B";
    [ObservableProperty] private string totalCombinedText = "0 B";
    [ObservableProperty] private string topTalkersText = "No activity";
    [ObservableProperty] private string availabilityMessage = string.Empty;
    [ObservableProperty] private string resultCountText = "No apps loaded";
    [ObservableProperty] private string filterSummary = "All apps • 24h • sorted by total";
    [ObservableProperty] private string lastUpdatedText = "Not refreshed yet";
    [ObservableProperty] private string usageInsightText = "Refresh usage to inspect app network activity.";
    [ObservableProperty] private bool showAccessButton;
    [ObservableProperty] private bool hasItems;
    [ObservableProperty] private bool hasNoItems = true;
    [ObservableProperty] private bool hasAvailabilityMessage;
    [ObservableProperty] private bool hasError;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand OpenAccessSettingsCommand { get; }

    public UsageViewModel(IAppNetworkUsageService usageService)
    {
        _usageService = usageService;

        Title = "Usage";
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        OpenAccessSettingsCommand = new AsyncRelayCommand(OpenAccessSettingsAsync);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedNetworkScopeChanged(string value) => _ = LoadAsync();

    partial void OnSelectedTimeRangeChanged(string value) => _ = LoadAsync();

    partial void OnSelectedSortChanged(string value) => ApplyFilters();

    partial void OnSelectedActivityFilterChanged(string value) => ApplyFilters();

    public async Task InitializeAsync()
    {
        if (UsageItems.Count == 0 && string.IsNullOrWhiteSpace(AvailabilityMessage))
        {
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        if (!await _loadGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var hasAccess = await _usageService.EnsureAccessAsync(cts.Token);

            var snapshot = await _usageService.GetUsageAsync(
                ParseScope(SelectedNetworkScope),
                ParseRange(SelectedTimeRange),
                cts.Token);

            _allItems = snapshot.Items.ToList();

            CurrentNetworkType = string.IsNullOrWhiteSpace(snapshot.CurrentNetworkType)
                ? "Unknown"
                : snapshot.CurrentNetworkType;

            AvailabilityMessage = snapshot.AvailabilityMessage ?? string.Empty;
            HasAvailabilityMessage = !string.IsNullOrWhiteSpace(AvailabilityMessage);
            ShowAccessButton = OperatingSystem.IsAndroid() && !hasAccess;

            TotalUploadText = FormatBytes(snapshot.TotalUploadBytes);
            TotalDownloadText = FormatBytes(snapshot.TotalDownloadBytes);
            TotalCombinedText = FormatBytes(snapshot.TotalUploadBytes + snapshot.TotalDownloadBytes);

            TopTalkersText = snapshot.TopTalkers.Count == 0
                ? "No active talkers"
                : string.Join(
                    ", ",
                    snapshot.TopTalkers
                        .Take(3)
                        .Select(x => string.Create(
                            InvariantCulture,
                            $"{x.AppName} ({FormatBytes(x.TotalBytes)})")));

            LastUpdatedText = string.Create(
                InvariantCulture,
                $"Updated {DateTimeOffset.Now:HH:mm:ss}");

            ApplyFilters();
        }
        catch (Exception ex)
        {
            SetError(string.IsNullOrWhiteSpace(ex.Message)
                ? "Failed to load app network usage."
                : ex.Message);

            _allItems.Clear();
            UsageItems.Clear();
            UpdateEmptyState();
        }
        finally
        {
            IsBusy = false;
            _loadGate.Release();
        }
    }

    private void ApplyFilters()
    {
        IEnumerable<AppNetworkUsageModel> query = _allItems;

        var normalizedSearch = SearchText?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(item =>
            {
                var packageId = ResolvePackageId(item);

                return Contains(item.AppName, normalizedSearch) ||
                       Contains(packageId, normalizedSearch);
            });
        }

        query = SelectedActivityFilter switch
        {
            "Active only" => query.Where(x => x.TotalBytes > 0),
            "100 KB+" => query.Where(x => x.TotalBytes >= 100L * 1024L),
            "1 MB+" => query.Where(x => x.TotalBytes >= 1024L * 1024L),
            "10 MB+" => query.Where(x => x.TotalBytes >= 10L * 1024L * 1024L),
            _ => query
        };

        query = SelectedSort switch
        {
            "Upload" => query.OrderByDescending(x => x.UploadBytes)
                             .ThenBy(x => x.AppName, StringComparer.OrdinalIgnoreCase),

            "Download" => query.OrderByDescending(x => x.DownloadBytes)
                               .ThenBy(x => x.AppName, StringComparer.OrdinalIgnoreCase),

            "App name" => query.OrderBy(x => x.AppName, StringComparer.OrdinalIgnoreCase),

            _ => query.OrderByDescending(x => x.TotalBytes)
                      .ThenBy(x => x.AppName, StringComparer.OrdinalIgnoreCase)
        };

        var materialized = query.ToList();
        var maxTotal = materialized.Count == 0 ? 0L : materialized.Max(x => x.TotalBytes);

        var rows = materialized
            .Select(item => UsageAppRowViewModel.FromModel(item, maxTotal))
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            UsageItems.Clear();

            foreach (var row in rows)
            {
                UsageItems.Add(row);
            }

            UpdateEmptyState();

            ResultCountText = string.Create(
                InvariantCulture,
                $"Showing {UsageItems.Count} of {_allItems.Count} app(s)");

            FilterSummary = string.Create(
                InvariantCulture,
                $"{SelectedNetworkScope} • {SelectedTimeRange} • {SelectedActivityFilter} • {SelectedSort}");

            UsageInsightText = BuildInsight(rows);
        });
    }

    private void UpdateEmptyState()
    {
        HasItems = UsageItems.Count > 0;
        HasNoItems = !HasItems;

        if (HasNoItems)
        {
            ResultCountText = _allItems.Count == 0
                ? "No app usage returned"
                : "No apps match the current filters.";
        }
    }

    private static string BuildInsight(List<UsageAppRowViewModel> rows)
    {
        if (rows.Count == 0)
        {
            return "No app usage matches the current view.";
        }

        var top = rows[0];

        if (top.TotalBytes == 0)
        {
            return "No network activity recorded in this window.";
        }

        return string.Create(
            InvariantCulture,
            $"{top.AppName} is the top talker with {top.TotalText} used.");
    }

    private async Task OpenAccessSettingsAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _usageService.OpenAccessSettingsAsync(cts.Token);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    private static UsageNetworkScope ParseScope(string value) =>
        value switch
        {
            "Wi-Fi" => UsageNetworkScope.Wifi,
            "Mobile" => UsageNetworkScope.Mobile,
            _ => UsageNetworkScope.All
        };

    private static UsageTimeRange ParseRange(string value) =>
        value switch
        {
            "7d" => UsageTimeRange.Last7Days,
            "30d" => UsageTimeRange.Last30Days,
            _ => UsageTimeRange.Last24Hours
        };

    private static bool Contains(string? source, string value)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolvePackageId(AppNetworkUsageModel item)
    {
        return ReadStringProperty(item, "PackageId") ??
               ReadStringProperty(item, "PackageName") ??
               ReadStringProperty(item, "BundleId") ??
               ReadStringProperty(item, "BundleIdentifier") ??
               ReadStringProperty(item, "AppIdentifier") ??
               string.Empty;
    }

    private static string ResolveLastActive(AppNetworkUsageModel item)
    {
        return ReadStringProperty(item, "LastActiveText") ??
               ReadStringProperty(item, "TimeframeBucket") ??
               ReadStringProperty(item, "LastActive") ??
               "Selected window";
    }

    private static string? ReadStringProperty(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        var value = property?.GetValue(source);

        return value?.ToString();
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? string.Create(InvariantCulture, $"{value:0} {units[unitIndex]}")
            : string.Create(InvariantCulture, $"{value:0.##} {units[unitIndex]}");
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = !string.IsNullOrWhiteSpace(message);
    }

    private void ClearError()
    {
        ErrorMessage = null;
        HasError = false;
    }

    public void Dispose()
    {
        _loadGate.Dispose();
    }

    public sealed class UsageAppRowViewModel
    {
        public required string AppName { get; init; }
        public required string PackageId { get; init; }
        public required string UploadText { get; init; }
        public required string DownloadText { get; init; }
        public required string TotalText { get; init; }
        public required string LastActiveText { get; init; }
        public required double TotalShareProgress { get; init; }
        public required long UploadBytes { get; init; }
        public required long DownloadBytes { get; init; }
        public required long TotalBytes { get; init; }

        public string PackageDisplay =>
            string.IsNullOrWhiteSpace(PackageId)
                ? "Package unavailable"
                : PackageId;

        public string Initial =>
            string.IsNullOrWhiteSpace(AppName)
                ? "?"
                : AppName.Trim()[0].ToString().ToUpperInvariant();

        public static UsageAppRowViewModel FromModel(AppNetworkUsageModel item, long maxTotal)
        {
            var total = Math.Max(0L, item.TotalBytes);
            var upload = Math.Max(0L, item.UploadBytes);
            var download = Math.Max(0L, item.DownloadBytes);

            var progress = maxTotal <= 0
                ? 0
                : Math.Clamp((double)total / maxTotal, 0, 1);

            return new UsageAppRowViewModel
            {
                AppName = string.IsNullOrWhiteSpace(item.AppName) ? "Unknown app" : item.AppName,
                PackageId = ResolvePackageId(item),
                UploadBytes = upload,
                DownloadBytes = download,
                TotalBytes = total,
                UploadText = FormatBytes(upload),
                DownloadText = FormatBytes(download),
                TotalText = FormatBytes(total),
                LastActiveText = ResolveLastActive(item),
                TotalShareProgress = progress
            };
        }
    }
}