#if WINDOWS
using Vyre.App.Models;

namespace Vyre.App.Services.Usage;

public sealed partial class AppNetworkUsageService
{
    private static partial bool GetShouldShowTab() => true;

    private static partial Task<bool> EnsureAccessCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(true);
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

        var snapshot = new AppNetworkUsageSnapshot
        {
            PlatformName = "Windows",
            CurrentNetworkType = Connectivity.Current.NetworkAccess == NetworkAccess.Internet ? "Connected" : "Offline",
            IsSupported = false,
            AvailabilityMessage = "This Windows build exposes the Usage tab but does not yet provide reliable cross-process per-app network attribution in the current MAUI packaging mode. Keep the tab visible for future packaged WinRT expansion.",
            Items = Array.Empty<AppNetworkUsageModel>()
        };

        return Task.FromResult(snapshot);
    }
}
#endif
