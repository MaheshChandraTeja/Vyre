using Microsoft.Extensions.Logging;
using Vyre.App.Models;

namespace Vyre.App.Services.Engine;

public sealed class VyreEngineService(ILogger<VyreEngineService> logger) : IVyreEngineService
{
    public Task<NativeBuildInfo> GetBuildInfoAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (NativeMethods.TryGetBuildInfo(out var buildInfo, out var error))
        {
            logger.LogInformation("Native build info retrieved successfully.");
            return Task.FromResult(new NativeBuildInfo(
                IsNativeAvailable: true,
                LibraryName: NativeMethods.LibraryName,
                Message: buildInfo,
                Source: "native"));
        }

        logger.LogWarning("Native build info unavailable: {Reason}", error);
        return Task.FromResult(new NativeBuildInfo(
            IsNativeAvailable: false,
            LibraryName: NativeMethods.LibraryName,
            Message: string.IsNullOrWhiteSpace(error)
                ? "Native library is not packaged yet for this runtime. Module 1 keeps the bridge shape ready so later modules can drop in platform binaries cleanly."
                : error,
            Source: "managed-fallback"));
    }
}
