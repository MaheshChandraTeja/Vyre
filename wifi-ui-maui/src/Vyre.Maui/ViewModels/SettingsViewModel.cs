using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyre.Maui.Models;
using Vyre.Maui.Services;

namespace Vyre.Maui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsStore _store;

    [ObservableProperty] private string interfaceSelection = "auto";
    [ObservableProperty] private int scanIntervalSeconds = 10;
    [ObservableProperty] private bool privacyHideBssids;
    [ObservableProperty] private bool privacyHideSsids;

    public SettingsViewModel(ISettingsStore store) => _store = store;

    [RelayCommand]
    public async Task LoadAsync()
    {
        var s = await _store.LoadAsync(CancellationToken.None);
        InterfaceSelection = s.InterfaceSelection;
        ScanIntervalSeconds = s.ScanIntervalSeconds;
        PrivacyHideBssids = s.PrivacyHideBssids;
        PrivacyHideSsids = s.PrivacyHideSsids;
        StatusMessage = "Loaded";
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (ScanIntervalSeconds < 2) ScanIntervalSeconds = 2;
        if (ScanIntervalSeconds > 120) ScanIntervalSeconds = 120;

        var s = new AppSettings(InterfaceSelection, ScanIntervalSeconds, PrivacyHideBssids, PrivacyHideSsids);
        await _store.SaveAsync(s, CancellationToken.None);
        StatusMessage = "Saved";
    }
}
