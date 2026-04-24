using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using Vyre.App.Models;
using Vyre.App.Services;

namespace Vyre.App.ViewModels;

public sealed partial class SettingsViewModel : BaseViewModel
{
    private const string RetentionDaysKey = "settings.retentionDays";
    private const string AutoRefreshKey = "settings.autoRefresh";
    private const string CompactCardsKey = "settings.compactCards";
    private const string DeveloperModeKey = "settings.developerMode";
    private const string LocalOnlyModeKey = "settings.localOnlyMode";

    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    private readonly ISettingsService _settingsService;

    public IReadOnlyList<string> InterfaceOptions { get; } =
        new[] { "Auto", "Wi-Fi", "Ethernet", "Virtual Adapter" };

    public IReadOnlyList<string> RetentionOptions { get; } =
        new[] { "7 days", "30 days", "90 days", "Forever" };

    public ObservableCollection<SettingsHealthItem> HealthItems { get; } = new();

    [ObservableProperty] private string selectedInterface = "Auto";
    [ObservableProperty] private int scanIntervalSeconds = 10;
    [ObservableProperty] private bool privacyModeEnabled = true;
    [ObservableProperty] private bool saveReportHistory = true;
    [ObservableProperty] private bool shareDiagnostics;
    [ObservableProperty] private bool autoRefreshEnabled = true;
    [ObservableProperty] private bool compactCardsEnabled;
    [ObservableProperty] private bool developerModeEnabled;
    [ObservableProperty] private bool localOnlyModeEnabled = true;
    [ObservableProperty] private string selectedRetention = "30 days";
    [ObservableProperty] private string saveStatus = string.Empty;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private int settingsScore = 92;
    [ObservableProperty] private double settingsScoreProgress = 0.92;
    [ObservableProperty] private string settingsScoreLabel = "Balanced";
    [ObservableProperty] private string profileSummary = "Privacy-first, local-only defaults with useful history enabled.";
    [ObservableProperty] private string intervalSummary = "Every 10 seconds";
    [ObservableProperty] private string retentionSummary = "Reports retained for 30 days.";
    [ObservableProperty] private string diagnosticsSummary = "Diagnostics sharing is off unless explicitly enabled.";

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand ResetDefaultsCommand { get; }
    public IAsyncRelayCommand ExportSnapshotCommand { get; }

    public IRelayCommand SetPrivacyPresetCommand { get; }
    public IRelayCommand SetBalancedPresetCommand { get; }
    public IRelayCommand SetPerformancePresetCommand { get; }

    public IRelayCommand SetInterval5Command { get; }
    public IRelayCommand SetInterval10Command { get; }
    public IRelayCommand SetInterval30Command { get; }
    public IRelayCommand SetInterval60Command { get; }

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        Title = "Settings";

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ResetDefaultsCommand = new AsyncRelayCommand(ResetDefaultsAsync);
        ExportSnapshotCommand = new AsyncRelayCommand(ExportSnapshotAsync);

        SetPrivacyPresetCommand = new RelayCommand(ApplyPrivacyPreset);
        SetBalancedPresetCommand = new RelayCommand(ApplyBalancedPreset);
        SetPerformancePresetCommand = new RelayCommand(ApplyPerformancePreset);

        SetInterval5Command = new RelayCommand(() => SetInterval(5));
        SetInterval10Command = new RelayCommand(() => SetInterval(10));
        SetInterval30Command = new RelayCommand(() => SetInterval(30));
        SetInterval60Command = new RelayCommand(() => SetInterval(60));

        RecalculateProfile();
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var settings = await _settingsService.GetAsync(cts.Token);

            SelectedInterface = settings.SelectedInterface;
            ScanIntervalSeconds = Math.Max(5, settings.ScanIntervalSeconds);
            PrivacyModeEnabled = settings.PrivacyModeEnabled;
            SaveReportHistory = settings.SaveReportHistory;
            ShareDiagnostics = settings.ShareDiagnostics;

            AutoRefreshEnabled = Preferences.Default.Get(AutoRefreshKey, true);
            CompactCardsEnabled = Preferences.Default.Get(CompactCardsKey, false);
            DeveloperModeEnabled = Preferences.Default.Get(DeveloperModeKey, false);
            LocalOnlyModeEnabled = Preferences.Default.Get(LocalOnlyModeKey, true);
            SelectedRetention = Preferences.Default.Get(RetentionDaysKey, "30 days");

            RecalculateProfile();
        }
        catch (Exception ex)
        {
            SetError($"Failed to load settings: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();

            ScanIntervalSeconds = Math.Clamp(ScanIntervalSeconds, 5, 3600);

            var settings = new AppSettings
            {
                SelectedInterface = SelectedInterface,
                ScanIntervalSeconds = ScanIntervalSeconds,
                PrivacyModeEnabled = PrivacyModeEnabled,
                SaveReportHistory = SaveReportHistory,
                ShareDiagnostics = ShareDiagnostics
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _settingsService.SaveAsync(settings, cts.Token);

            Preferences.Default.Set(AutoRefreshKey, AutoRefreshEnabled);
            Preferences.Default.Set(CompactCardsKey, CompactCardsEnabled);
            Preferences.Default.Set(DeveloperModeKey, DeveloperModeEnabled);
            Preferences.Default.Set(LocalOnlyModeKey, LocalOnlyModeEnabled);
            Preferences.Default.Set(RetentionDaysKey, SelectedRetention);

            SaveStatus = string.Create(
                InvariantCulture,
                $"Saved at {DateTimeOffset.Now:HH:mm:ss}");

            RecalculateProfile();
        }
        catch (Exception ex)
        {
            SetError($"Failed to save settings: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResetDefaultsAsync()
    {
        SelectedInterface = "Auto";
        ScanIntervalSeconds = 10;
        PrivacyModeEnabled = true;
        SaveReportHistory = true;
        ShareDiagnostics = false;
        AutoRefreshEnabled = true;
        CompactCardsEnabled = false;
        DeveloperModeEnabled = false;
        LocalOnlyModeEnabled = true;
        SelectedRetention = "30 days";

        await SaveAsync();
        SaveStatus = "Defaults restored and saved.";
    }

    private void ApplyPrivacyPreset()
    {
        PrivacyModeEnabled = true;
        ShareDiagnostics = false;
        SaveReportHistory = false;
        LocalOnlyModeEnabled = true;
        AutoRefreshEnabled = false;
        DeveloperModeEnabled = false;
        SelectedRetention = "7 days";
        ScanIntervalSeconds = 30;
        RecalculateProfile();
        SaveStatus = "Privacy preset applied. Save to persist.";
    }

    private void ApplyBalancedPreset()
    {
        PrivacyModeEnabled = true;
        ShareDiagnostics = false;
        SaveReportHistory = true;
        LocalOnlyModeEnabled = true;
        AutoRefreshEnabled = true;
        DeveloperModeEnabled = false;
        SelectedRetention = "30 days";
        ScanIntervalSeconds = 10;
        RecalculateProfile();
        SaveStatus = "Balanced preset applied. Save to persist.";
    }

    private void ApplyPerformancePreset()
    {
        PrivacyModeEnabled = false;
        ShareDiagnostics = false;
        SaveReportHistory = true;
        LocalOnlyModeEnabled = true;
        AutoRefreshEnabled = true;
        DeveloperModeEnabled = true;
        SelectedRetention = "90 days";
        ScanIntervalSeconds = 5;
        RecalculateProfile();
        SaveStatus = "Performance preset applied. Save to persist.";
    }

    private void SetInterval(int seconds)
    {
        ScanIntervalSeconds = Math.Clamp(seconds, 5, 3600);
        RecalculateProfile();
    }

    private async Task ExportSnapshotAsync()
    {
        try
        {
            var snapshot = BuildSnapshot();
            var directory = Path.Combine(FileSystem.Current.AppDataDirectory, "settings");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(
                directory,
                string.Create(InvariantCulture, $"vyre-settings-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.txt"));

            await File.WriteAllTextAsync(path, snapshot);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Share Vyre Settings Snapshot",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            SetError($"Settings snapshot export failed: {ex.Message}");
        }
    }

    private string BuildSnapshot()
    {
        var builder = new StringBuilder();

        builder.AppendLine("Vyre Settings Snapshot");
        builder.AppendLine("======================");
        builder.AppendLine(string.Create(InvariantCulture, $"Generated: {DateTimeOffset.UtcNow:O}"));
        builder.AppendLine(string.Create(InvariantCulture, $"Profile Score: {SettingsScore}/100"));
        builder.AppendLine(string.Create(InvariantCulture, $"Profile: {SettingsScoreLabel}"));
        builder.AppendLine();

        builder.AppendLine("Core");
        builder.AppendLine("----");
        builder.AppendLine(string.Create(InvariantCulture, $"Interface: {SelectedInterface}"));
        builder.AppendLine(string.Create(InvariantCulture, $"Scan Interval: {ScanIntervalSeconds} seconds"));
        builder.AppendLine(string.Create(InvariantCulture, $"Auto Refresh: {AutoRefreshEnabled}"));
        builder.AppendLine();

        builder.AppendLine("Privacy & Storage");
        builder.AppendLine("-----------------");
        builder.AppendLine(string.Create(InvariantCulture, $"Privacy Mode: {PrivacyModeEnabled}"));
        builder.AppendLine(string.Create(InvariantCulture, $"Local Only Mode: {LocalOnlyModeEnabled}"));
        builder.AppendLine(string.Create(InvariantCulture, $"Save Report History: {SaveReportHistory}"));
        builder.AppendLine(string.Create(InvariantCulture, $"Retention: {SelectedRetention}"));
        builder.AppendLine(string.Create(InvariantCulture, $"Share Diagnostics: {ShareDiagnostics}"));
        builder.AppendLine();

        builder.AppendLine("Interface");
        builder.AppendLine("---------");
        builder.AppendLine(string.Create(InvariantCulture, $"Compact Cards: {CompactCardsEnabled}"));
        builder.AppendLine(string.Create(InvariantCulture, $"Developer Mode: {DeveloperModeEnabled}"));

        return builder.ToString();
    }

    private void RecalculateProfile()
    {
        var score = 100;

        if (!PrivacyModeEnabled)
        {
            score -= 14;
        }

        if (ShareDiagnostics)
        {
            score -= 12;
        }

        if (!LocalOnlyModeEnabled)
        {
            score -= 18;
        }

        if (!SaveReportHistory)
        {
            score -= 5;
        }

        if (ScanIntervalSeconds <= 5)
        {
            score -= 8;
        }
        else if (ScanIntervalSeconds >= 60)
        {
            score -= 4;
        }

        if (DeveloperModeEnabled)
        {
            score -= 6;
        }

        SettingsScore = Math.Clamp(score, 0, 100);
        SettingsScoreProgress = SettingsScore / 100.0;

        SettingsScoreLabel = SettingsScore switch
        {
            >= 90 => "Hardened",
            >= 78 => "Balanced",
            >= 62 => "Tuned",
            _ => "Risky"
        };

        IntervalSummary = ScanIntervalSeconds switch
        {
            <= 5 => "Very frequent scanning. Fast feedback, higher battery cost.",
            <= 15 => string.Create(InvariantCulture, $"Every {ScanIntervalSeconds} seconds. Good live visibility."),
            <= 60 => string.Create(InvariantCulture, $"Every {ScanIntervalSeconds} seconds. Conservative and battery-friendly."),
            _ => string.Create(InvariantCulture, $"Every {ScanIntervalSeconds} seconds. Very light background activity.")
        };

        RetentionSummary = SaveReportHistory
            ? string.Create(InvariantCulture, $"Reports retained for {SelectedRetention.ToLowerInvariant()}.")
            : "Report history is disabled. New reports will not be archived.";

        DiagnosticsSummary = ShareDiagnostics
            ? "Diagnostics sharing is enabled, but only when explicitly used."
            : "Diagnostics sharing is disabled. Local data stays local.";

        ProfileSummary = BuildProfileSummary();

        HealthItems.Clear();
        HealthItems.Add(new SettingsHealthItem(
            "Privacy posture",
            PrivacyModeEnabled && LocalOnlyModeEnabled ? "Strong" : "Needs review",
            PrivacyModeEnabled && LocalOnlyModeEnabled
                ? "Sensitive network details are handled cautiously and kept local."
                : "Privacy/local-only protections are relaxed.",
            PrivacyModeEnabled && LocalOnlyModeEnabled ? "#22C55E" : "#F59E0B"));

        HealthItems.Add(new SettingsHealthItem(
            "Scan behavior",
            ScanIntervalSeconds <= 15 ? "Live" : "Conservative",
            IntervalSummary,
            ScanIntervalSeconds <= 15 ? "#A78BFA" : "#38BDF8"));

        HealthItems.Add(new SettingsHealthItem(
            "History policy",
            SaveReportHistory ? "Enabled" : "Off",
            RetentionSummary,
            SaveReportHistory ? "#22C55E" : "#64748B"));

        HealthItems.Add(new SettingsHealthItem(
            "Diagnostics",
            ShareDiagnostics ? "Share-ready" : "Private",
            DiagnosticsSummary,
            ShareDiagnostics ? "#F59E0B" : "#22C55E"));
    }

    private string BuildProfileSummary()
    {
        if (PrivacyModeEnabled && LocalOnlyModeEnabled && !ShareDiagnostics)
        {
            return "Privacy-first configuration with local processing and controlled diagnostics.";
        }

        if (ScanIntervalSeconds <= 5)
        {
            return "High-frequency scanning profile for active troubleshooting and fast feedback.";
        }

        if (!SaveReportHistory)
        {
            return "Low-retention profile. Useful when you want minimal stored scan history.";
        }

        return "Balanced operational profile for daily scanning, reports, and diagnostics.";
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

    partial void OnSelectedInterfaceChanged(string value) => RecalculateProfile();
    partial void OnScanIntervalSecondsChanged(int value) => RecalculateProfile();
    partial void OnPrivacyModeEnabledChanged(bool value) => RecalculateProfile();
    partial void OnSaveReportHistoryChanged(bool value) => RecalculateProfile();
    partial void OnShareDiagnosticsChanged(bool value) => RecalculateProfile();
    partial void OnAutoRefreshEnabledChanged(bool value) => RecalculateProfile();
    partial void OnCompactCardsEnabledChanged(bool value) => RecalculateProfile();
    partial void OnDeveloperModeEnabledChanged(bool value) => RecalculateProfile();
    partial void OnLocalOnlyModeEnabledChanged(bool value) => RecalculateProfile();
    partial void OnSelectedRetentionChanged(string value) => RecalculateProfile();
}

public sealed record SettingsHealthItem(
    string Title,
    string Status,
    string Detail,
    string AccentColor);