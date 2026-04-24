using Vyre.App.Models;

namespace Vyre.App.Services.Wifi;

public sealed partial class PlatformWifiScanProvider : IPlatformWifiScanProvider
{
    public Task<PlatformWifiScanPayload> ScanAsync(CancellationToken cancellationToken) =>
        ScanCoreAsync(cancellationToken);

    public Task<string> GetCapabilitySummaryAsync(CancellationToken cancellationToken) =>
        GetCapabilitySummaryCoreAsync(cancellationToken);

    private static partial Task<PlatformWifiScanPayload> ScanCoreAsync(CancellationToken cancellationToken);
    private static partial Task<string> GetCapabilitySummaryCoreAsync(CancellationToken cancellationToken);

    internal static int FrequencyToChannel(int frequencyMhz)
    {
        if (frequencyMhz == 2484)
        {
            return 14;
        }

        if (frequencyMhz is >= 2412 and <= 2472)
        {
            return (frequencyMhz - 2407) / 5;
        }

        if (frequencyMhz is >= 5000 and <= 5895)
        {
            return (frequencyMhz - 5000) / 5;
        }

        if (frequencyMhz is >= 5955 and <= 7115)
        {
            return (frequencyMhz - 5950) / 5;
        }

        return 0;
    }

    internal static string FrequencyToBand(int frequencyMhz)
    {
        if (frequencyMhz is >= 2400 and < 2500)
        {
            return "2.4 GHz";
        }

        if (frequencyMhz is >= 4900 and < 5925)
        {
            return "5 GHz";
        }

        if (frequencyMhz is >= 5925 and < 7125)
        {
            return "6 GHz";
        }

        return "Unknown";
    }
}
