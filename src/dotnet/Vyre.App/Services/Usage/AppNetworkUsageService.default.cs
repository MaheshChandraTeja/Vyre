#if !ANDROID && !IOS && !WINDOWS
using Vyre.App.Models;

namespace Vyre.App.Services.Usage;

public sealed partial class AppNetworkUsageService
{
    private static partial bool GetShouldShowTab() => true;

    private static partial Task<bool> EnsureAccessCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }

    private static partial Task OpenAccessSettingsCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static partial Task<AppNetworkUsageSnapshot> GetUsageCoreAsync(
        UsageNetworkScope networkScope,
        UsageTimeRange timeRange,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new AppNetworkUsageSnapshot
        {
            PlatformName = DeviceInfo.Current.Platform.ToString(),
            CurrentNetworkType = Connectivity.Current.NetworkAccess == NetworkAccess.Internet ? "Connected" : "Offline",
            IsSupported = false,
            AvailabilityMessage = "Per-app network usage is not implemented for this platform in the current build.",
            Items = Array.Empty<AppNetworkUsageModel>()
        });
    }
}
#endif
