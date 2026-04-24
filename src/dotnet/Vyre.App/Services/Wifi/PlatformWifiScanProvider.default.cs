#if !ANDROID && !IOS
using Vyre.App.Models;

namespace Vyre.App.Services.Wifi;

public sealed partial class PlatformWifiScanProvider
{
    private static partial Task<PlatformWifiScanPayload> ScanCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = new PlatformWifiScanPayload
        {
            SourcePlatform = DeviceInfo.Current.Platform.ToString(),
            IsPartial = true,
            CapabilityMessage = "Platform-specific scanner is not implemented for this target in Milestones 5 and 6.",
            Warnings = new[]
            {
                "Falling back to previously implemented platform-specific scanning where available."
            },
            AccessPoints = Array.Empty<AccessPointViewData>()
        };

        return Task.FromResult(payload);
    }

    private static partial Task<string> GetCapabilitySummaryCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult("Platform-specific scanner is not implemented for this target in Milestones 5 and 6.");
    }
}
#endif
