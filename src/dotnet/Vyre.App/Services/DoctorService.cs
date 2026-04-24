using Vyre.App.Models;
using Vyre.App.Services.Diagnostics;
using Vyre.App.Services.Engine;
using Vyre.App.Services.Wifi;

namespace Vyre.App.Services;

public sealed class DoctorService : IDoctorService
{
    private readonly IVyreEngineService _engineService;
    private readonly IPlatformWifiScanProvider _platformWifiScanProvider;
    private readonly INetworkDiagnosticsService _networkDiagnosticsService;

    public DoctorService(
        IVyreEngineService engineService,
        IPlatformWifiScanProvider platformWifiScanProvider,
        INetworkDiagnosticsService networkDiagnosticsService)
    {
        _engineService = engineService;
        _platformWifiScanProvider = platformWifiScanProvider;
        _networkDiagnosticsService = networkDiagnosticsService;
    }

    public async Task<DoctorStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var runtime = DeviceInfo.Current.Platform.ToString();
        var storagePath = FileSystem.Current.AppDataDirectory;

        string nativeVersion;
        string nativeStatus;

        try
        {
            nativeVersion = await _engineService.GetVersionAsync(cancellationToken);
            nativeStatus = string.IsNullOrWhiteSpace(nativeVersion) ? "Unavailable" : "Loaded";
        }
        catch (Exception ex)
        {
            nativeVersion = $"Unavailable ({ex.GetType().Name})";
            nativeStatus = "Load failed";
        }

        string platformScannerAvailability;
        try
        {
            platformScannerAvailability = await _platformWifiScanProvider.GetCapabilitySummaryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            platformScannerAvailability = $"Capability probe failed: {ex.Message}";
        }

        NetworkDiagnosticSnapshot diagnostics;
        try
        {
            diagnostics = await _networkDiagnosticsService.RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            diagnostics = new NetworkDiagnosticSnapshot
            {
                ReachabilityStatus = $"Diagnostics failed: {ex.Message}",
                DnsStatus = "DNS diagnostics unavailable.",
                LatencyStatus = "Latency diagnostics unavailable.",
                LocalNetworkStatus = "Local network diagnostics unavailable.",
                CurrentNetworkMessage = "Unknown"
            };
        }

        return new DoctorStatus
        {
            Runtime = runtime,
            NativeInteropStatus = nativeStatus,
            NativeVersion = nativeVersion,
            PermissionsStatus = platformScannerAvailability,
            StorageStatus = Directory.Exists(storagePath)
                ? "Writable app data directory available."
                : "App data directory unavailable.",
            Notes = string.Join(" ", diagnostics.Notes),
            PlatformScannerAvailability = platformScannerAvailability,
            ReachabilityStatus = diagnostics.ReachabilityStatus,
            DnsStatus = diagnostics.DnsStatus,
            LatencyStatus = diagnostics.LatencyStatus,
            LocalNetworkStatus = diagnostics.LocalNetworkStatus,
            CurrentNetworkMessage = diagnostics.CurrentNetworkMessage
        };
    }
}
