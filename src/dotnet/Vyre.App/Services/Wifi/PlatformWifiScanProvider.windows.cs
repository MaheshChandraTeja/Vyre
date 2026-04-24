#if WINDOWS
using System.Diagnostics;
using System.Globalization;
using Vyre.App.Models;

namespace Vyre.App.Services.Wifi;

public sealed partial class PlatformWifiScanProvider
{
    private const int NetshTimeoutSeconds = 8;

    private static async partial Task<PlatformWifiScanPayload> ScanCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var networkResult = await RunNetshAsync("wlan show networks mode=bssid", cancellationToken);
        var interfaceResult = await RunNetshAsync("wlan show interfaces", cancellationToken);
        var warnings = new List<string>();

        if (!networkResult.IsSuccess)
        {
            warnings.Add(networkResult.Message);

            return new PlatformWifiScanPayload
            {
                SourcePlatform = "Windows",
                IsPartial = true,
                CurrentSsid = interfaceResult.IsSuccess ? TryParseCurrentSsid(interfaceResult.Output) : null,
                CapabilityMessage = networkResult.Message,
                Warnings = warnings,
                AccessPoints = Array.Empty<AccessPointViewData>()
            };
        }

        var accessPoints = ParseNetworks(networkResult.Output, warnings)
            .OrderByDescending(x => x.SignalDbm)
            .ThenBy(x => x.Ssid, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (accessPoints.Count > 0)
        {
            warnings.Add("Windows reports signal as quality percent; Vyre converts it to an estimated dBm value.");
        }
        else
        {
            warnings.Add("Windows returned no visible access points. Confirm Wi-Fi is enabled and the WLAN AutoConfig service is running.");
        }

        return new PlatformWifiScanPayload
        {
            SourcePlatform = "Windows",
            IsPartial = true,
            CurrentSsid = interfaceResult.IsSuccess ? TryParseCurrentSsid(interfaceResult.Output) : null,
            CapabilityMessage = "Windows scan uses the WLAN service through netsh. Signal strength is estimated from Windows quality percentages.",
            Warnings = warnings,
            AccessPoints = accessPoints
        };
    }

    private static partial Task<string> GetCapabilitySummaryCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            "Windows: nearby AP visibility is available when a Wi-Fi adapter is present, Wi-Fi is enabled, and WLAN AutoConfig is running. Signal dBm values are estimated from Windows quality percentages.");
    }

    private static async Task<NetshResult> RunNetshAsync(
        string arguments,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(NetshTimeoutSeconds));

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("netsh.exe", arguments)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };

        try
        {
            if (!process.Start())
            {
                return NetshResult.Failed("Windows Wi-Fi scan command could not be started.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

            await process.WaitForExitAsync(timeoutCts.Token);

            var output = await outputTask;
            var error = await errorTask;
            var combined = string.Join(
                Environment.NewLine,
                new[] { output, error }.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (process.ExitCode == 0)
            {
                return NetshResult.Succeeded(combined);
            }

            return NetshResult.Failed(FormatNetshFailure(combined));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return NetshResult.Failed("Windows Wi-Fi scan timed out while waiting for netsh.");
        }
        catch (Exception ex)
        {
            return NetshResult.Failed($"Windows Wi-Fi scan failed: {ex.Message}");
        }
    }

    private static List<AccessPointViewData> ParseNetworks(
        string output,
        List<string> warnings)
    {
        var accessPoints = new List<AccessPointViewData>();
        var currentSsid = string.Empty;
        var currentSecurity = "Unknown";
        var currentBssid = string.Empty;
        var currentBand = string.Empty;
        int? currentChannel = null;
        int? currentSignalPercent = null;

        foreach (var rawLine in output.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("SSID ", StringComparison.OrdinalIgnoreCase))
            {
                AddCurrentBssid();
                currentSsid = ValueAfterColon(line);
                currentSecurity = "Unknown";
                continue;
            }

            if (line.StartsWith("Authentication", StringComparison.OrdinalIgnoreCase))
            {
                currentSecurity = NormalizeSecurity(ValueAfterColon(line));
                continue;
            }

            if (line.StartsWith("BSSID ", StringComparison.OrdinalIgnoreCase))
            {
                AddCurrentBssid();
                currentBssid = ValueAfterColon(line);
                currentBand = string.Empty;
                currentChannel = null;
                currentSignalPercent = null;
                continue;
            }

            if (line.StartsWith("Signal", StringComparison.OrdinalIgnoreCase))
            {
                currentSignalPercent = ParsePercent(ValueAfterColon(line));
                continue;
            }

            if (line.StartsWith("Band", StringComparison.OrdinalIgnoreCase))
            {
                currentBand = NormalizeBand(ValueAfterColon(line));
                continue;
            }

            if (line.StartsWith("Channel", StringComparison.OrdinalIgnoreCase))
            {
                currentChannel = ParseInt(ValueAfterColon(line));
            }
        }

        AddCurrentBssid();

        if (accessPoints.Count == 0 &&
            output.Contains("There are 0 networks", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Windows WLAN service is available, but it currently sees no nearby networks.");
        }

        return accessPoints;

        void AddCurrentBssid()
        {
            if (string.IsNullOrWhiteSpace(currentBssid))
            {
                return;
            }

            var channel = currentChannel ?? 0;
            var band = string.IsNullOrWhiteSpace(currentBand)
                ? ChannelToBand(channel)
                : currentBand;
            var frequencyMhz = ChannelToFrequency(channel, band);

            accessPoints.Add(new AccessPointViewData
            {
                Ssid = string.IsNullOrWhiteSpace(currentSsid) ? "<Hidden>" : currentSsid,
                Bssid = currentBssid,
                Band = band,
                Channel = channel,
                FrequencyMhz = frequencyMhz,
                SignalDbm = EstimateDbm(currentSignalPercent),
                Security = currentSecurity,
                IsPartialObservation = true
            });

            currentBssid = string.Empty;
            currentBand = string.Empty;
            currentChannel = null;
            currentSignalPercent = null;
        }
    }

    private static string? TryParseCurrentSsid(string output)
    {
        foreach (var rawLine in output.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) &&
                !line.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase))
            {
                var ssid = ValueAfterColon(line);
                return string.IsNullOrWhiteSpace(ssid) ? null : ssid;
            }
        }

        return null;
    }

    private static string FormatNetshFailure(string output)
    {
        if (output.Contains("wlansvc", StringComparison.OrdinalIgnoreCase) &&
            output.Contains("not running", StringComparison.OrdinalIgnoreCase))
        {
            return "Windows Wi-Fi scanning needs the WLAN AutoConfig service. Start WLAN AutoConfig and try again.";
        }

        if (output.Contains("no wireless interface", StringComparison.OrdinalIgnoreCase))
        {
            return "Windows did not find a Wi-Fi adapter. Connect or enable a wireless adapter and try again.";
        }

        var firstLine = output
            .ReplaceLineEndings("\n")
            .Split('\n')
            .Select(x => x.Trim())
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        return string.IsNullOrWhiteSpace(firstLine)
            ? "Windows Wi-Fi scan failed before returning details."
            : $"Windows Wi-Fi scan failed: {firstLine}";
    }

    private static string ValueAfterColon(string line)
    {
        var colonIndex = line.IndexOf(':', StringComparison.Ordinal);
        return colonIndex < 0 ? string.Empty : line[(colonIndex + 1)..].Trim();
    }

    private static int? ParsePercent(string value)
    {
        var normalized = value.Replace("%", string.Empty, StringComparison.Ordinal).Trim();
        return int.TryParse(
            normalized,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? Math.Clamp(parsed, 0, 100)
            : null;
    }

    private static int? ParseInt(string value)
    {
        return int.TryParse(
            value.Trim(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }

    private static int EstimateDbm(int? signalPercent)
    {
        return signalPercent is null ? 0 : (signalPercent.Value / 2) - 100;
    }

    private static string NormalizeSecurity(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        if (value.Contains("Open", StringComparison.OrdinalIgnoreCase))
        {
            return "Open";
        }

        return value.Trim();
    }

    private static string NormalizeBand(string value)
    {
        if (value.Contains("2.4", StringComparison.OrdinalIgnoreCase))
        {
            return "2.4 GHz";
        }

        if (value.Contains('6'))
        {
            return "6 GHz";
        }

        if (value.Contains('5'))
        {
            return "5 GHz";
        }

        return "Unknown";
    }

    private static string ChannelToBand(int channel)
    {
        if (channel is >= 1 and <= 14)
        {
            return "2.4 GHz";
        }

        if (channel is >= 32 and <= 177)
        {
            return "5 GHz";
        }

        return "Unknown";
    }

    private static int ChannelToFrequency(int channel, string band)
    {
        if (channel == 14 && string.Equals(band, "2.4 GHz", StringComparison.OrdinalIgnoreCase))
        {
            return 2484;
        }

        if (channel is >= 1 and <= 13 &&
            string.Equals(band, "2.4 GHz", StringComparison.OrdinalIgnoreCase))
        {
            return 2407 + (channel * 5);
        }

        if (string.Equals(band, "5 GHz", StringComparison.OrdinalIgnoreCase))
        {
            return 5000 + (channel * 5);
        }

        if (string.Equals(band, "6 GHz", StringComparison.OrdinalIgnoreCase))
        {
            return 5950 + (channel * 5);
        }

        return 0;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cleanup after timeout.
        }
    }

    private sealed record NetshResult(
        bool IsSuccess,
        string Output,
        string Message)
    {
        public static NetshResult Succeeded(string output) => new(true, output, string.Empty);

        public static NetshResult Failed(string message) => new(false, string.Empty, message);
    }
}
#endif
