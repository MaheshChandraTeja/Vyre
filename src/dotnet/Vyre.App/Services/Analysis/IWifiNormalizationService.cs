using Vyre.App.Models;

namespace Vyre.App.Services.Analysis;

public interface IWifiNormalizationService
{
    Task<IReadOnlyList<AccessPointViewData>> NormalizeAsync(
        IReadOnlyList<AccessPointViewData> accessPoints,
        string sourcePlatform,
        bool isPartial,
        CancellationToken cancellationToken);
}