using Vyre.App.Core.Models;

namespace Vyre.App.Core.Services;

public static class BootstrapStatusFormatter
{
    public static string FormatHeadline(EngineBridgeStatus status) =>
        status.IsNativeAvailable
            ? "Native bridge is live and reachable."
            : "Managed shell is healthy, native bridge is not loaded yet.";

    public static string FormatDetail(EngineBridgeStatus status) =>
        status.IsNativeAvailable
            ? $"Source: {status.Source}. Library: {status.LibraryName}. Payload: {status.Message}"
            : $"Source: {status.Source}. Expected library: {status.LibraryName}. Reason: {status.Message}";
}
