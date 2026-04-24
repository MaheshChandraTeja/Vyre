using Vyre.App.Models;

namespace Vyre.App.Services.Usage;

public sealed partial class AppNetworkUsageService : IAppNetworkUsageService
{
    public bool ShouldShowTab => GetShouldShowTab();

    public Task<AppNetworkUsageSnapshot> GetUsageAsync(
        UsageNetworkScope networkScope,
        UsageTimeRange timeRange,
        CancellationToken cancellationToken) =>
        GetUsageCoreAsync(networkScope, timeRange, cancellationToken);

    public Task<bool> EnsureAccessAsync(CancellationToken cancellationToken) =>
        EnsureAccessCoreAsync(cancellationToken);

    public Task OpenAccessSettingsAsync(CancellationToken cancellationToken) =>
        OpenAccessSettingsCoreAsync(cancellationToken);

    private static partial Task<AppNetworkUsageSnapshot> GetUsageCoreAsync(
        UsageNetworkScope networkScope,
        UsageTimeRange timeRange,
        CancellationToken cancellationToken);

    private static partial Task<bool> EnsureAccessCoreAsync(CancellationToken cancellationToken);

    private static partial Task OpenAccessSettingsCoreAsync(CancellationToken cancellationToken);

    private static partial bool GetShouldShowTab();

    internal static (DateTimeOffset Start, DateTimeOffset End) ResolveRange(UsageTimeRange range)
    {
        var end = DateTimeOffset.UtcNow;
        var start = range switch
        {
            UsageTimeRange.Last7Days => end.AddDays(-7),
            UsageTimeRange.Last30Days => end.AddDays(-30),
            _ => end.AddDays(-1)
        };

        return (start, end);
    }

    internal static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
}
