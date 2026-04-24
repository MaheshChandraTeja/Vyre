#if IOS
using Vyre.App.Models;

namespace Vyre.App.Services.Usage;

public sealed partial class AppNetworkUsageService
{
    private static partial bool GetShouldShowTab() => false;

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
            PlatformName = "iOS",
            CurrentNetworkType = Connectivity.Current.NetworkAccess == NetworkAccess.Internet ? "Connected" : "Offline",
            IsSupported = false,
            AvailabilityMessage = "iOS does not expose other apps' network usage to normal third-party apps, so this tab is hidden on iOS.",
            Items = Array.Empty<AppNetworkUsageModel>()
        });
    }
}
#endif
