using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Vyre.App.Services;

namespace Vyre.App.ViewModels;

public sealed partial class DoctorViewModel : BaseViewModel
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    private readonly IDoctorService _doctorService;

    public ObservableCollection<DoctorSignalItem> DeviceSignals { get; } = new();
    public ObservableCollection<DoctorSignalItem> NetworkSignals { get; } = new();
    public ObservableCollection<DoctorSignalItem> VyreSignals { get; } = new();
    public ObservableCollection<DoctorRemedyItem> Remedies { get; } = new();

    [ObservableProperty] private string runtime = string.Empty;
    [ObservableProperty] private string nativeInteropStatus = string.Empty;
    [ObservableProperty] private string nativeVersion = string.Empty;
    [ObservableProperty] private string permissionsStatus = string.Empty;
    [ObservableProperty] private string storageStatus = string.Empty;
    [ObservableProperty] private string notes = string.Empty;

    [ObservableProperty] private string platformScannerAvailability = string.Empty;
    [ObservableProperty] private string reachabilityStatus = string.Empty;
    [ObservableProperty] private string dnsStatus = string.Empty;
    [ObservableProperty] private string latencyStatus = string.Empty;
    [ObservableProperty] private string localNetworkStatus = string.Empty;
    [ObservableProperty] private string currentNetworkMessage = string.Empty;

    [ObservableProperty] private string deviceSummary = "Device diagnostics have not run yet.";
    [ObservableProperty] private string networkSummary = "Network diagnostics have not run yet.";
    [ObservableProperty] private string vyreSummary = "Vyre runtime diagnostics have not run yet.";

    [ObservableProperty] private string platformLabel = DeviceInfo.Platform.ToString();
    [ObservableProperty] private string deviceModel = DeviceInfo.Model;
    [ObservableProperty] private string deviceManufacturer = DeviceInfo.Manufacturer;
    [ObservableProperty] private string osVersion = DeviceInfo.VersionString;
    [ObservableProperty] private string idiomLabel = DeviceInfo.Idiom.ToString();

    [ObservableProperty] private int healthScore;
    [ObservableProperty] private double healthProgress;
    [ObservableProperty] private string healthSummary = "Run Doctor to calculate health.";
    [ObservableProperty] private string healthLabel = "UNKNOWN";

    [ObservableProperty] private int passedCount;
    [ObservableProperty] private int warningCount;
    [ObservableProperty] private int criticalCount;

    [ObservableProperty] private string lastCheckedText = "Not checked";
    [ObservableProperty] private string remedyCountText = "0";
    [ObservableProperty] private bool hasNoRemedies = true;
    [ObservableProperty] private bool hasError;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand ExportSnapshotCommand { get; }

    public DoctorViewModel(IDoctorService doctorService)
    {
        _doctorService = doctorService;

        Title = "Doctor";
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        ExportSnapshotCommand = new AsyncRelayCommand(ExportSnapshotAsync);
    }

    public async Task InitializeAsync()
    {
        if (DeviceSignals.Count == 0 && NetworkSignals.Count == 0 && VyreSignals.Count == 0)
        {
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(18));
            var status = await _doctorService.GetStatusAsync(cts.Token);

            Runtime = Safe(status.Runtime);
            NativeInteropStatus = Safe(status.NativeInteropStatus);
            NativeVersion = Safe(status.NativeVersion);
            PermissionsStatus = Safe(status.PermissionsStatus);
            StorageStatus = Safe(status.StorageStatus);
            Notes = Safe(status.Notes);

            PlatformScannerAvailability = Safe(status.PlatformScannerAvailability);
            ReachabilityStatus = Safe(status.ReachabilityStatus);
            DnsStatus = Safe(status.DnsStatus);
            LatencyStatus = Safe(status.LatencyStatus);
            LocalNetworkStatus = Safe(status.LocalNetworkStatus);
            CurrentNetworkMessage = Safe(status.CurrentNetworkMessage);

            PlatformLabel = DeviceInfo.Platform.ToString();
            DeviceModel = Safe(DeviceInfo.Model);
            DeviceManufacturer = Safe(DeviceInfo.Manufacturer);
            OsVersion = Safe(DeviceInfo.VersionString);
            IdiomLabel = DeviceInfo.Idiom.ToString();

            LastCheckedText = DateTimeOffset.Now.ToString("HH:mm:ss", InvariantCulture);

            BuildDeviceSignals();
            BuildNetworkSignals();
            BuildVyreSignals();
            BuildRemedies();
            CalculateHealth();
            BuildSectionSummaries();
        }
        catch (Exception ex)
        {
            SetError(string.Create(InvariantCulture, $"Doctor check failed: {ex.Message}"));

            DeviceSignals.Clear();
            NetworkSignals.Clear();
            VyreSignals.Clear();
            Remedies.Clear();

            VyreSignals.Add(DoctorSignalItem.Fail(
                "Doctor Service",
                string.Create(InvariantCulture, $"Diagnostic execution failed: {ex.Message}")));

            Remedies.Add(new DoctorRemedyItem(
                "P0",
                "Fix Doctor service execution",
                "Check IDoctorService registration, platform diagnostics services, and MauiProgram.cs dependency wiring.",
                "Doctor cannot diagnose the app if Doctor itself is unconscious. Peak software comedy."));

            HasNoRemedies = false;
            RemedyCountText = Remedies.Count.ToString(InvariantCulture);

            CalculateHealth();
            BuildSectionSummaries();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildDeviceSignals()
    {
        DeviceSignals.Clear();

        DeviceSignals.Add(DoctorSignalItem.Pass("Platform", PlatformLabel));
        DeviceSignals.Add(DoctorSignalItem.Pass("Device", string.Create(InvariantCulture, $"{DeviceManufacturer} {DeviceModel}")));
        DeviceSignals.Add(DoctorSignalItem.Pass("OS version", OsVersion));
        DeviceSignals.Add(DoctorSignalItem.Pass("Form factor", IdiomLabel));
        DeviceSignals.Add(BuildSignal("Storage", StorageStatus));
    }

    private void BuildNetworkSignals()
    {
        NetworkSignals.Clear();

        NetworkSignals.Add(BuildSignal("Current network", CurrentNetworkMessage));
        NetworkSignals.Add(BuildSignal("Reachability", ReachabilityStatus));
        NetworkSignals.Add(BuildSignal("DNS", DnsStatus));
        NetworkSignals.Add(BuildSignal("Latency", LatencyStatus));
        NetworkSignals.Add(BuildSignal("Local interfaces", LocalNetworkStatus));
    }

    private void BuildVyreSignals()
    {
        VyreSignals.Clear();

        VyreSignals.Add(BuildSignal("Native interop", NativeInteropStatus));
        VyreSignals.Add(BuildSignal("Native core", NativeVersion));
        VyreSignals.Add(BuildSignal("Scanner capability", PlatformScannerAvailability));
        VyreSignals.Add(BuildSignal("Permissions", PermissionsStatus));

        if (!string.IsNullOrWhiteSpace(Notes) && !Notes.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            VyreSignals.Add(BuildSignal("Platform notes", Notes));
        }
    }

    private void BuildRemedies()
    {
        Remedies.Clear();

        AddRemedyIfBad(
            "P0",
            "Native bridge needs attention",
            NativeInteropStatus,
            "Rebuild wifi-core and wifi-interop. Verify native libraries are copied into the platform output.",
            "Without native interop, scanner normalization and report generation can degrade.");

        AddRemedyIfBad(
            "P0",
            "Storage is not healthy",
            StorageStatus,
            "Check app data directory access and available storage. Clear corrupted local app data if needed.",
            "Doctor, reports, capture files, and snapshots all depend on local storage.");

        AddRemedyIfBad(
            "P1",
            "Scanner capability is limited",
            PlatformScannerAvailability,
            "Open permissions and confirm Wi-Fi/location requirements. On iOS, expect scanner limits unless entitlements exist.",
            "Different OSes expose different Wi-Fi APIs. Vyre should be honest instead of inventing access points like a tiny fraudster.");

        AddRemedyIfBad(
            "P1",
            "Permissions may be blocking diagnostics",
            PermissionsStatus,
            "Grant required Wi-Fi/location/network permissions, then run Doctor again.",
            "Android and iOS love hiding useful network details behind permission gates.");

        AddRemedyIfBad(
            "P2",
            "Network reachability is degraded",
            ReachabilityStatus,
            "Reconnect Wi-Fi, check emulator/device internet access, or test another network.",
            "Reachability failures make scan enrichment and diagnostics unreliable.");

        AddRemedyIfBad(
            "P2",
            "DNS diagnostics are degraded",
            DnsStatus,
            "Try another network, check resolver configuration, or retry after reconnecting.",
            "DNS failures can make the app look broken when the network is the actual gremlin.");

        AddRemedyIfBad(
            "P2",
            "Latency probe is degraded",
            LatencyStatus,
            "Move closer to the AP, reduce network load, or compare against another network.",
            "High latency can indicate weak signal, congestion, captive portals, or adapter instability.");

        AddRemedyIfBad(
            "P3",
            "Local network visibility is limited",
            LocalNetworkStatus,
            "Check LAN permissions, guest network isolation, VPNs, or emulator network mode.",
            "Local diagnostics need visible interfaces. Guest networks often break this while acting smug.");

        HasNoRemedies = Remedies.Count == 0;
        RemedyCountText = Remedies.Count.ToString(InvariantCulture);
    }

    private void CalculateHealth()
    {
        var allSignals = DeviceSignals
            .Concat(NetworkSignals)
            .Concat(VyreSignals)
            .ToList();

        PassedCount = allSignals.Count(x => x.Level == "PASS");
        WarningCount = allSignals.Count(x => x.Level == "WARN");
        CriticalCount = allSignals.Count(x => x.Level == "FAIL");

        var score = 100;
        score -= CriticalCount * 20;
        score -= WarningCount * 8;

        if (HasError)
        {
            score -= 25;
        }

        HealthScore = Math.Clamp(score, 0, 100);
        HealthProgress = HealthScore / 100.0;

        HealthLabel = HealthScore switch
        {
            >= 90 => "STRONG",
            >= 75 => "GOOD",
            >= 55 => "DEGRADED",
            _ => "CRITICAL"
        };

        HealthSummary = HealthScore switch
        {
            >= 90 => "Device, network, and Vyre runtime look healthy.",
            >= 75 => "Mostly healthy, with a few platform or permission warnings.",
            >= 55 => "Usable, but network or runtime diagnostics need attention.",
            _ => "Serious issues detected. Fix P0/P1 remedies before trusting scans."
        };
    }

    private void BuildSectionSummaries()
    {
        DeviceSummary = BuildSummary(DeviceSignals, "Device environment is ready.", "Device environment has warnings.", "Device environment has critical issues.");
        NetworkSummary = BuildSummary(NetworkSignals, "Network diagnostics look stable.", "Network diagnostics are partially degraded.", "Network diagnostics have critical failures.");
        VyreSummary = BuildSummary(VyreSignals, "Vyre runtime is healthy.", "Vyre runtime has warnings.", "Vyre runtime has critical failures.");
    }

    private static string BuildSummary(
        IEnumerable<DoctorSignalItem> signals,
        string pass,
        string warn,
        string fail)
    {
        var list = signals.ToList();

        if (list.Any(x => x.Level == "FAIL"))
        {
            return fail;
        }

        if (list.Any(x => x.Level == "WARN"))
        {
            return warn;
        }

        return pass;
    }

    private static DoctorSignalItem BuildSignal(string name, string detail)
    {
        return Classify(detail) switch
        {
            DiagnosticLevel.Pass => DoctorSignalItem.Pass(name, detail),
            DiagnosticLevel.Warn => DoctorSignalItem.Warn(name, detail),
            _ => DoctorSignalItem.Fail(name, detail)
        };
    }

    private static DiagnosticLevel Classify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DiagnosticLevel.Warn;
        }

        var text = value.ToLowerInvariant();

        string[] critical =
        [
            "failed",
            "failure",
            "error",
            "missing",
            "not found",
            "unavailable",
            "denied",
            "disabled",
            "not supported",
            "unsupported",
            "blocked",
            "cannot",
            "no adapter",
            "no device",
            "not reachable"
        ];

        string[] warnings =
        [
            "limited",
            "partial",
            "unknown",
            "warning",
            "degraded",
            "restricted",
            "not granted",
            "not available",
            "timeout",
            "slow"
        ];

        if (critical.Any(text.Contains))
        {
            return DiagnosticLevel.Critical;
        }

        if (warnings.Any(text.Contains))
        {
            return DiagnosticLevel.Warn;
        }

        return DiagnosticLevel.Pass;
    }

    private void AddRemedyIfBad(
        string priority,
        string title,
        string diagnosticText,
        string action,
        string whyItMatters)
    {
        if (Classify(diagnosticText) == DiagnosticLevel.Pass)
        {
            return;
        }

        Remedies.Add(new DoctorRemedyItem(priority, title, action, whyItMatters));
    }

    private async Task ExportSnapshotAsync()
    {
        try
        {
            var snapshot = BuildSnapshot();
            var directory = Path.Combine(FileSystem.Current.AppDataDirectory, "doctor");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(
                directory,
                string.Create(InvariantCulture, $"vyre-doctor-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.txt"));

            await File.WriteAllTextAsync(path, snapshot);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Share Vyre Doctor Snapshot",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            SetError(string.Create(InvariantCulture, $"Snapshot export failed: {ex.Message}"));
        }
    }

    private string BuildSnapshot()
    {
        var builder = new StringBuilder();

        builder.AppendLine("Vyre Doctor Snapshot");
        builder.AppendLine("====================");
        builder.AppendLine(string.Create(InvariantCulture, $"Generated: {DateTimeOffset.UtcNow:O}"));
        builder.AppendLine(string.Create(InvariantCulture, $"Health: {HealthScore}/100 {HealthLabel}"));
        builder.AppendLine(string.Create(InvariantCulture, $"Platform: {PlatformLabel}"));
        builder.AppendLine(string.Create(InvariantCulture, $"Device: {DeviceManufacturer} {DeviceModel}"));
        builder.AppendLine(string.Create(InvariantCulture, $"OS: {OsVersion}"));
        builder.AppendLine();

        AppendSection(builder, "Device", DeviceSignals);
        AppendSection(builder, "Network", NetworkSignals);
        AppendSection(builder, "Vyre", VyreSignals);

        builder.AppendLine("Remedies");
        builder.AppendLine("--------");

        if (Remedies.Count == 0)
        {
            builder.AppendLine("No remedies required.");
        }
        else
        {
            foreach (var remedy in Remedies)
            {
                builder.AppendLine(string.Create(InvariantCulture, $"{remedy.Priority} {remedy.Title}"));
                builder.AppendLine(string.Create(InvariantCulture, $"Action: {remedy.Action}"));
                builder.AppendLine(string.Create(InvariantCulture, $"Why: {remedy.WhyItMatters}"));
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static void AppendSection(
        StringBuilder builder,
        string title,
        IEnumerable<DoctorSignalItem> items)
    {
        builder.AppendLine(title);
        builder.AppendLine(new string('-', title.Length));

        foreach (var item in items)
        {
            builder.AppendLine(string.Create(InvariantCulture, $"[{item.Level}] {item.Name}: {item.Detail}"));
        }

        builder.AppendLine();
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

    private static string Safe(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
    }

    private enum DiagnosticLevel
    {
        Pass,
        Warn,
        Critical
    }
}

public sealed record DoctorSignalItem(
    string Name,
    string Detail,
    string Level,
    string AccentColor)
{
    public static DoctorSignalItem Pass(string name, string detail)
        => new(name, detail, "PASS", "#22C55E");

    public static DoctorSignalItem Warn(string name, string detail)
        => new(name, detail, "WARN", "#F59E0B");

    public static DoctorSignalItem Fail(string name, string detail)
        => new(name, detail, "FAIL", "#EF4444");
}

public sealed record DoctorRemedyItem(
    string Priority,
    string Title,
    string Action,
    string WhyItMatters);