using System.Text;
using Microsoft.Extensions.Logging;
using Vyre.App.Models;

namespace Vyre.App.Services.Engine;

public sealed partial class VyreEngineService(ILogger<VyreEngineService> logger) : IVyreEngineService
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Native version unavailable: {Reason}")]
    private static partial void LogNativeVersionUnavailable(ILogger logger, string? reason);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Native build info unavailable: {Reason}")]
    private static partial void LogNativeBuildInfoUnavailable(ILogger logger, string? reason);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Native scan start failed: {Reason}")]
    private static partial void LogNativeScanStartFailed(ILogger logger, string? reason);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Native scan results failed: {Reason}")]
    private static partial void LogNativeScanResultsFailed(ILogger logger, string? reason);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Native JSON analysis failed: {Reason}")]
    private static partial void LogNativeJsonAnalysisFailed(ILogger logger, string? reason);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Native interop diagnostics completed successfully with {Count} access points.")]
    private static partial void LogInteropDiagnosticsCompleted(ILogger logger, int count);

    public Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(NativeMethods.GetVersionSafe());
    }

    public Task<string> SubmitScanResultsJsonAsync(string scanResultsJson, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return NativeMethods.SubmitScanResultsJson(scanResultsJson);
        }, cancellationToken);
    }
    
    public Task<NativeInteropSnapshot> RunInteropDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var version = NativeMethods.GetVersionSafe();
        if (string.IsNullOrWhiteSpace(version))
        {
            var versionError = NativeMethods.GetLastErrorMessage();
            LogNativeVersionUnavailable(logger, versionError);
            return Task.FromResult(NativeInteropSnapshot.CreateUnavailable(
                NativeMethods.LibraryName,
                string.IsNullOrWhiteSpace(versionError)
                    ? "Native library is not packaged yet for this runtime."
                    : versionError));
        }

        var buildInfo = NativeMethods.GetBuildInfoSafe();
        if (string.IsNullOrWhiteSpace(buildInfo))
        {
            var buildInfoError = NativeMethods.GetLastErrorMessage();
            LogNativeBuildInfoUnavailable(logger, buildInfoError);
            return Task.FromResult(NativeInteropSnapshot.CreateUnavailable(
                NativeMethods.LibraryName,
                string.IsNullOrWhiteSpace(buildInfoError)
                    ? "Build info could not be read from the native library."
                    : buildInfoError));
        }

        var scanResult = NativeMethods.ScanOnce();
        if (scanResult.StatusCode != NativeMethods.NativeStatusCode.Ok)
        {
            LogNativeScanResultsFailed(logger, scanResult.ErrorMessage);
            return Task.FromResult(NativeInteropSnapshot.CreateUnavailable(NativeMethods.LibraryName, scanResult.ErrorMessage));
        }

        var accessPoints = scanResult.Items
            .Select(item => new NativeAccessPoint(
                Bssid: item.Bssid,
                Ssid: item.Ssid,
                Channel: item.Channel,
                RssiDbm: item.SignalDbm,
                FrequencyMhz: BandToFrequency(item.Band, item.Channel),
                Security: item.Security,
                Hidden: string.IsNullOrWhiteSpace(item.Ssid)))
            .ToList();

        var scanJson = BuildScanJson(accessPoints);
        if (!NativeMethods.TryAnalyzeJson(scanJson, out var reportJson, out var analyzeError))
        {
            LogNativeJsonAnalysisFailed(logger, analyzeError);
            return Task.FromResult(NativeInteropSnapshot.CreateUnavailable(NativeMethods.LibraryName, analyzeError));
        }

        LogInteropDiagnosticsCompleted(logger, accessPoints.Count);

        return Task.FromResult(new NativeInteropSnapshot(
            IsNativeAvailable: true,
            LibraryName: NativeMethods.LibraryName,
            Version: version,
            BuildInfo: buildInfo,
            ReportJson: reportJson,
            Source: "native",
            Message: "GetVersion and AnalyzeJson completed successfully through the C ABI boundary.",
            AccessPoints: accessPoints));
    }

    private static string BuildScanJson(List<NativeAccessPoint> accessPoints)
    {
        var builder = new StringBuilder();
        builder.Append('[');

        for (var index = 0; index < accessPoints.Count; index++)
        {
            var accessPoint = accessPoints[index];
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append('{')
                .Append("\"bssid\":\"").Append(EscapeJson(accessPoint.Bssid)).Append("\",")
                .Append("\"ssid\":\"").Append(EscapeJson(accessPoint.Ssid)).Append("\",")
                .Append("\"channel\":").Append(accessPoint.Channel).Append(',')
                .Append("\"rssiDbm\":").Append(accessPoint.RssiDbm).Append(',')
                .Append("\"frequencyMhz\":").Append(accessPoint.FrequencyMhz).Append(',')
                .Append("\"security\":\"").Append(EscapeJson(accessPoint.Security)).Append("\",")
                .Append("\"hidden\":").Append(accessPoint.Hidden ? "true" : "false")
                .Append('}');
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string EscapeJson(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);

    private static int BandToFrequency(string band, int channel)
    {
        return band switch
        {
            "2.4 GHz" => channel == 14 ? 2484 : 2407 + (channel * 5),
            "5 GHz" => 5000 + (channel * 5),
            "6 GHz" => 5950 + (channel * 5),
            _ => 0
        };
    }
}
