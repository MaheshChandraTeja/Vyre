using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyre.Maui.Models;
using Vyre.Maui.Services;

namespace Vyre.Maui.ViewModels;

public partial class ScanViewModel : BaseViewModel
{
    private readonly IWifiEngine _engine;
    private readonly ISettingsStore _settingsStore;

    private CancellationTokenSource? _cts;

    public ObservableCollection<AccessPointModel> AccessPoints { get; } = new();
    public ObservableCollection<AccessPointModel> Filtered { get; } = new();

    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private bool show24Ghz = true;
    [ObservableProperty] private bool show5Ghz = true;
    [ObservableProperty] private bool show6Ghz = true;
    [ObservableProperty] private bool hideHiddenSsids = false;

    [ObservableProperty] private string sortMode = "Signal";

    public ScanViewModel(IWifiEngine engine, ISettingsStore settingsStore)
    {
        _engine = engine;
        _settingsStore = settingsStore;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnShow24GhzChanged(bool value) => ApplyFilters();
    partial void OnShow5GhzChanged(bool value) => ApplyFilters();
    partial void OnShow6GhzChanged(bool value) => ApplyFilters();
    partial void OnHideHiddenSsidsChanged(bool value) => ApplyFilters();
    partial void OnSortModeChanged(string value) => ApplyFilters();

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Scanning…";

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            var aps = await _engine.ScanAsync(_cts.Token);

            AccessPoints.Clear();
            foreach (var ap in aps)
                AccessPoints.Add(ap);

            ApplyFilters();
            StatusMessage = $"Found {Filtered.Count} APs";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        IEnumerable<AccessPointModel> q = AccessPoints;

        if (hideHiddenSsids)
            q = q.Where(a => !string.IsNullOrWhiteSpace(a.Ssid));

        if (!show24Ghz) q = q.Where(a => a.Band != "2.4ghz");
        if (!show5Ghz) q = q.Where(a => a.Band != "5ghz");
        if (!show6Ghz) q = q.Where(a => a.Band != "6ghz");

        var term = SearchText?.Trim() ?? "";
        if (!string.IsNullOrEmpty(term))
        {
            q = q.Where(a =>
                a.Ssid.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.Bssid.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        q = SortMode switch
        {
            "SSID" => q.OrderBy(a => a.Ssid).ThenBy(a => a.Bssid),
            "Band" => q.OrderBy(a => a.Band).ThenBy(a => a.Ssid),
            _      => q.OrderByDescending(a => a.RssiDbm ?? int.MinValue).ThenBy(a => a.Ssid)
        };

        Filtered.Clear();
        foreach (var ap in q)
            Filtered.Add(ap);
    }
}
