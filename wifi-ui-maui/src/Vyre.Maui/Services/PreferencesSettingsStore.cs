using Vyre.Maui.Models;

namespace Vyre.Maui.Services;

public sealed class PreferencesSettingsStore : ISettingsStore
{
    private const string KeyInterface = "settings.interface";
    private const string KeyInterval = "settings.scan_interval_sec";
    private const string KeyHideBssids = "settings.privacy_hide_bssids";
    private const string KeyHideSsids = "settings.privacy_hide_ssids";

    public Task<AppSettings> LoadAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var s = new AppSettings(
            InterfaceSelection: Preferences.Get(KeyInterface, AppSettings.Default.InterfaceSelection),
            ScanIntervalSeconds: Preferences.Get(KeyInterval, AppSettings.Default.ScanIntervalSeconds),
            PrivacyHideBssids: Preferences.Get(KeyHideBssids, AppSettings.Default.PrivacyHideBssids),
            PrivacyHideSsids: Preferences.Get(KeyHideSsids, AppSettings.Default.PrivacyHideSsids)
        );

        return Task.FromResult(s);
    }

    public Task SaveAsync(AppSettings settings, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Preferences.Set(KeyInterface, settings.InterfaceSelection);
        Preferences.Set(KeyInterval, settings.ScanIntervalSeconds);
        Preferences.Set(KeyHideBssids, settings.PrivacyHideBssids);
        Preferences.Set(KeyHideSsids, settings.PrivacyHideSsids);

        return Task.CompletedTask;
    }
}
