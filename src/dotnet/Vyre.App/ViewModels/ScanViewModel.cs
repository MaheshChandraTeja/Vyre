using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyre.App.Models;
using Vyre.App.Services.Reports;
using Vyre.App.Services.Wifi;

namespace Vyre.App.ViewModels;

public sealed class ScanViewModel : BaseViewModel, IDisposable
{
    private readonly IWifiScanService _wifiScanService;
    private readonly IReportArchiveService _reportArchiveService;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    private List<AccessPointViewData> _allItems = new();
    private string _searchText = string.Empty;
    private string _selectedSort = "Signal";
    private string _selectedBand = "All";
    private bool _showOnlyOpenNetworks;
    private string _scanMetaText = string.Empty;

    public ObservableCollection<AccessPointViewData> AccessPoints { get; } = new();

    public IReadOnlyList<string> SortOptions { get; } = new[] { "Signal", "SSID", "Channel", "Vendor" };
    public IReadOnlyList<string> BandOptions { get; } = new[] { "All", "2.4 GHz", "5 GHz", "6 GHz", "Unknown" };

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SelectedSort
    {
        get => _selectedSort;
        set
        {
            if (SetProperty(ref _selectedSort, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SelectedBand
    {
        get => _selectedBand;
        set
        {
            if (SetProperty(ref _selectedBand, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool ShowOnlyOpenNetworks
    {
        get => _showOnlyOpenNetworks;
        set
        {
            if (SetProperty(ref _showOnlyOpenNetworks, value))
            {
                ApplyFilters();
            }
        }
    }

    public string ScanMetaText
    {
        get => _scanMetaText;
        set => SetProperty(ref _scanMetaText, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }

    public ScanViewModel(IWifiScanService wifiScanService, IReportArchiveService reportArchiveService)
    {
        _wifiScanService = wifiScanService;
        _reportArchiveService = reportArchiveService;
        Title = "Scan";
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task InitializeAsync()
    {
        if (AccessPoints.Count == 0)
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
            ErrorMessage = null;

            using var cts = new CancellationTokenSource();
            var snapshot = await _wifiScanService.ScanAndAnalyzeAsync(cts.Token);

            _allItems = snapshot.AccessPoints?.ToList() ?? new List<AccessPointViewData>();
            ApplyFilters();

            var warningSuffix = snapshot.Warnings is { Count: > 0 }
                ? $" • {snapshot.Warnings.Count} warning(s)"
                : string.Empty;

            ScanMetaText =
                $"{snapshot.SourcePlatform} • {_allItems.Count} AP(s)"
                + (snapshot.IsPartial ? " • partial visibility" : string.Empty)
                + warningSuffix;

            if (_allItems.Count == 0)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(snapshot.CapabilityMessage)
                    ? "No access points were found."
                    : snapshot.CapabilityMessage;
            }

            try
            {
                await _reportArchiveService.SaveLatestReportAsync(CancellationToken.None);
            }
            catch
            {
                // Non-fatal. A scan should still succeed even if archive persistence fails.
            }
        }
        catch (Exception ex)
        {
            _allItems.Clear();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AccessPoints.Clear();
            });

            ScanMetaText = string.Empty;
            ErrorMessage = string.IsNullOrWhiteSpace(ex.Message)
                ? "Scan failed."
                : ex.Message;
        }
        finally
        {
            IsBusy = false;
            _loadGate.Release();
        }
    }

    private void ApplyFilters()
    {
        IEnumerable<AccessPointViewData> query = _allItems;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();

            query = query.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Ssid) && x.Ssid.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Bssid) && x.Bssid.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Vendor) && x.Vendor.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Security) && x.Security.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.Equals(SelectedBand, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => string.Equals(x.Band, SelectedBand, StringComparison.OrdinalIgnoreCase));
        }

        if (ShowOnlyOpenNetworks)
        {
            query = query.Where(x => x.IsOpen);
        }

        query = SelectedSort switch
        {
            "SSID" => query
                .OrderBy(x => x.Ssid, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(x => x.SignalDbm),

            "Channel" => query
                .OrderBy(x => x.Channel)
                .ThenByDescending(x => x.SignalDbm)
                .ThenBy(x => x.Ssid, StringComparer.OrdinalIgnoreCase),

            "Vendor" => query
                .OrderBy(x => string.IsNullOrWhiteSpace(x.Vendor) ? "ZZZZZZZZ" : x.Vendor, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(x => x.SignalDbm)
                .ThenBy(x => x.Ssid, StringComparer.OrdinalIgnoreCase),

            _ => query
                .OrderByDescending(x => x.SignalDbm)
                .ThenBy(x => x.Ssid, StringComparer.OrdinalIgnoreCase)
        };

        var materialized = query.ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            AccessPoints.Clear();
            foreach (var item in materialized)
            {
                AccessPoints.Add(item);
            }
        });
    }

    public void Dispose()
    {
        _loadGate.Dispose();
    }
}