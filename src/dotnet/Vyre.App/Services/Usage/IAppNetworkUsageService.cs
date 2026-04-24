using Vyre.App.Models;

namespace Vyre.App.Services.Usage;

public interface IAppNetworkUsageService
{
    Task<AppNetworkUsageSnapshot> GetUsageAsync(
        UsageNetworkScope networkScope,
        UsageTimeRange timeRange,
        CancellationToken cancellationToken);

    Task<bool> EnsureAccessAsync(CancellationToken cancellationToken);

    Task OpenAccessSettingsAsync(CancellationToken cancellationToken);

    bool ShouldShowTab { get; }
}