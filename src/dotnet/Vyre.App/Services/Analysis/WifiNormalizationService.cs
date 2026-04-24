using Vyre.App.Models;

namespace Vyre.App.Services.Analysis;

public sealed class WifiNormalizationService : IWifiNormalizationService
{
    private readonly IOuiVendorLookupService _ouiVendorLookupService;

    public WifiNormalizationService(IOuiVendorLookupService ouiVendorLookupService)
    {
        _ouiVendorLookupService = ouiVendorLookupService;
    }

    public async Task<IReadOnlyList<AccessPointViewData>> NormalizeAsync(
        IReadOnlyList<AccessPointViewData> accessPoints,
        string sourcePlatform,
        bool isPartial,
        CancellationToken cancellationToken)
    {
        var normalized = new List<AccessPointViewData>(accessPoints.Count);

        foreach (var ap in accessPoints)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedChannel = NormalizeChannel(ap.Channel, ap.FrequencyMhz);
            var normalizedBand = NormalizeBand(ap.Band, ap.FrequencyMhz, normalizedChannel);
            var securityCategory = NormalizeSecurityCategory(ap.Security);
            var securityDisplay = NormalizeSecurityDisplay(ap.Security, securityCategory);
            var vendor = await _ouiVendorLookupService.LookupVendorAsync(ap.Bssid, cancellationToken);

            normalized.Add(new AccessPointViewData
            {
                Bssid = ap.Bssid,
                Ssid = string.IsNullOrWhiteSpace(ap.Ssid) ? "<Hidden>" : ap.Ssid,
                Band = normalizedBand,
                Channel = normalizedChannel,
                SignalDbm = ap.SignalDbm,
                Security = securityDisplay,
                FrequencyMhz = ap.FrequencyMhz,
                Vendor = vendor,
                SecurityCategory = securityCategory,
                ConfidenceScore = ComputeConfidenceScore(sourcePlatform, isPartial, ap.Bssid, normalizedChannel, ap.FrequencyMhz, ap.Security),
                IsPartialObservation = isPartial
            });
        }

        return normalized
            .OrderByDescending(x => x.SignalDbm)
            .ThenBy(x => x.Ssid, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeBand(string band, int frequencyMhz, int channel)
    {
        // Frequency is the source of truth. Trust physics before trusting labels.
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

        if (!string.IsNullOrWhiteSpace(band))
        {
            var normalized = band.Trim();

            if (normalized.Equals("2.4GHz", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("2.4 Ghz", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("2.4 GHz", StringComparison.OrdinalIgnoreCase))
            {
                return "2.4 GHz";
            }

            if (normalized.Equals("5GHz", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("5 Ghz", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("5 GHz", StringComparison.OrdinalIgnoreCase))
            {
                return "5 GHz";
            }

            if (normalized.Equals("6GHz", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("6 Ghz", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("6 GHz", StringComparison.OrdinalIgnoreCase))
            {
                return "6 GHz";
            }
        }

        // Last-resort inference from channel. This is weaker than frequency,
        // but better than branding a channel 36 AP as 2.4 GHz like a tiny liar.
        if (channel is >= 32 and <= 196)
        {
            return "5 GHz";
        }

        if (channel is >= 1 and <= 14)
        {
            return "2.4 GHz";
        }

        return "Unknown";
    }

    private static int NormalizeChannel(int channel, int frequencyMhz)
    {
        if (channel > 0)
        {
            return channel;
        }

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

    private static string NormalizeSecurityCategory(string rawSecurity)
    {
        if (string.IsNullOrWhiteSpace(rawSecurity))
        {
            return "Unknown";
        }

        var value = rawSecurity.ToUpperInvariant();

        if (value.Contains("OWE") || value.Contains("ENHANCED OPEN"))
        {
            return "Enhanced Open";
        }

        if (value.Contains("WPA3") || value.Contains("SAE"))
        {
            return "WPA3";
        }

        if (value.Contains("RSN") || value.Contains("WPA2"))
        {
            return value.Contains("EAP") || value.Contains("ENTERPRISE") ? "Enterprise" : "WPA2";
        }

        if (value.Contains("WPA"))
        {
            return value.Contains("EAP") || value.Contains("ENTERPRISE") ? "Enterprise" : "WPA";
        }

        if (value.Contains("WEP"))
        {
            return "WEP";
        }

        if (value == "OPEN" || value == "[ESS]" || value.Contains("NONE"))
        {
            return "Open";
        }

        return "Unknown";
    }

    private static string NormalizeSecurityDisplay(string rawSecurity, string category)
    {
        return category switch
        {
            "Open" => "Open",
            "WEP" => "WEP",
            "Enhanced Open" => "Enhanced Open (OWE)",
            "WPA3" => "WPA3",
            "WPA2" => "WPA2",
            "WPA" => "WPA",
            "Enterprise" => rawSecurity.Contains("WPA3", StringComparison.OrdinalIgnoreCase) ? "WPA3 Enterprise" :
                            rawSecurity.Contains("WPA2", StringComparison.OrdinalIgnoreCase) || rawSecurity.Contains("RSN", StringComparison.OrdinalIgnoreCase) ? "WPA2 Enterprise" :
                            "Enterprise",
            _ => string.IsNullOrWhiteSpace(rawSecurity) ? "Unknown" : rawSecurity
        };
    }

    private static double ComputeConfidenceScore(string sourcePlatform, bool isPartial, string bssid, int channel, int frequencyMhz, string rawSecurity)
    {
        var score = 0.75;

        if (sourcePlatform.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            score = 0.97;
        }
        else if (sourcePlatform.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            score = 0.93;
        }
        else if (sourcePlatform.Contains("iOS", StringComparison.OrdinalIgnoreCase))
        {
            score = 0.52;
        }

        if (isPartial)
        {
            score -= 0.22;
        }

        if (string.IsNullOrWhiteSpace(bssid))
        {
            score -= 0.18;
        }

        if (channel <= 0 && frequencyMhz <= 0)
        {
            score -= 0.12;
        }

        if (string.IsNullOrWhiteSpace(rawSecurity))
        {
            score -= 0.08;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }
}