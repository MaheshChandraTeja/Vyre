using System.Collections.ObjectModel;

namespace Vyre.App.Models;

public sealed record NativeInteropSnapshot(
    bool IsNativeAvailable,
    string LibraryName,
    string Version,
    string BuildInfo,
    string ReportJson,
    string Source,
    string Message,
    IReadOnlyList<NativeAccessPoint> AccessPoints)
{
    public static NativeInteropSnapshot CreateUnavailable(string libraryName, string message) =>
        new(
            IsNativeAvailable: false,
            LibraryName: libraryName,
            Version: string.Empty,
            BuildInfo: string.Empty,
            ReportJson: string.Empty,
            Source: "managed-fallback",
            Message: message,
            AccessPoints: Array.Empty<NativeAccessPoint>());
}
