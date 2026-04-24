namespace Vyre.App.Models;

public sealed class DoctorStatus
{
    public string Runtime { get; init; } = string.Empty;
    public string NativeInteropStatus { get; init; } = string.Empty;
    public string NativeVersion { get; init; } = string.Empty;
    public string PermissionsStatus { get; init; } = string.Empty;
    public string StorageStatus { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;

    public string PlatformScannerAvailability { get; init; } = string.Empty;
    public string ReachabilityStatus { get; init; } = string.Empty;
    public string DnsStatus { get; init; } = string.Empty;
    public string LatencyStatus { get; init; } = string.Empty;
    public string LocalNetworkStatus { get; init; } = string.Empty;
    public string CurrentNetworkMessage { get; init; } = string.Empty;
}