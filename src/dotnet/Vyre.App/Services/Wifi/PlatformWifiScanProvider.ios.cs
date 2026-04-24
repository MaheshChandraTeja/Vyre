#if IOS
using Vyre.App.Models;

namespace Vyre.App.Services.Wifi;

public sealed partial class PlatformWifiScanProvider
{
    private static partial Task<PlatformWifiScanPayload> ScanCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var warnings = new List<string>
        {
            "iOS does not expose full nearby Wi-Fi scan results to normal apps.",
            "SSID visibility may require Apple entitlements and user-approved conditions."
        };

        var payload = new PlatformWifiScanPayload
        {
            SourcePlatform = "iOS",
            IsPartial = true,
            CurrentSsid = null,
            CapabilityMessage = "iOS supports diagnostics and limited network context, not full AP enumeration.",
            Warnings = warnings,
            AccessPoints = Array.Empty<AccessPointViewData>()
        };

        return Task.FromResult(payload);
    }

    private static partial Task<string> GetCapabilitySummaryCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            "iOS: full Wi-Fi AP scanning is not available to standard apps. Diagnostics, reachability, DNS, latency, and limited current-network context are supported.");
    }
}
#endif
