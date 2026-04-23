using Vyre.App.Models;

namespace Vyre.App.Services.Engine;

public interface IVyreEngineService
{
    Task<NativeBuildInfo> GetBuildInfoAsync(CancellationToken cancellationToken = default);
}
