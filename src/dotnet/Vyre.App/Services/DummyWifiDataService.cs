using Vyre.App.Models;

namespace Vyre.App.Services;

public sealed class DummyWifiDataService : IDummyWifiDataService
{
    public async Task<IReadOnlyList<AccessPointViewData>> GetAccessPointsAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(300, cancellationToken);

        return new List<AccessPointViewData>
        {
            new() { Ssid = "Kairais-5G", Bssid = "D8:11:90:10:22:31", Band = "5 GHz", Channel = 44, SignalDbm = -48, Security = "WPA3" },
            new() { Ssid = "Kairais-IoT", Bssid = "D8:11:90:10:22:32", Band = "2.4 GHz", Channel = 6, SignalDbm = -65, Security = "WPA2" },
            new() { Ssid = "Cafe_Open", Bssid = "14:22:10:45:88:01", Band = "2.4 GHz", Channel = 11, SignalDbm = -71, Security = "Open" },
            new() { Ssid = "Office-West", Bssid = "90:AB:CD:55:EE:10", Band = "5 GHz", Channel = 149, SignalDbm = -59, Security = "WPA2" },
            new() { Ssid = "Mesh-Node-3", Bssid = "7C:33:AA:08:10:77", Band = "6 GHz", Channel = 5, SignalDbm = -62, Security = "WPA3" },
            new() { Ssid = "Legacy-Lab", Bssid = "F0:FF:AB:91:00:CC", Band = "2.4 GHz", Channel = 1, SignalDbm = -83, Security = "WEP" }
        };
    }

    public async Task<(IReadOnlyList<InsightIssue> Issues, IReadOnlyList<InsightRecommendation> Recommendations)> GetInsightsAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(250, cancellationToken);

        var issues = new List<InsightIssue>
        {
            new() { Code = "OPEN_NET", Severity = "High", Title = "Open network detected", Description = "At least one network is broadcasting without encryption." },
            new() { Code = "WEAK_SEC", Severity = "High", Title = "Weak security protocol", Description = "A WEP network was detected. This is not acceptable in 2026, yet here we are." },
            new() { Code = "LOW_SIGNAL", Severity = "Medium", Title = "Low signal quality", Description = "One or more access points have weak RSSI and may cause unreliable connectivity." },
            new() { Code = "CHANNEL_DENSITY", Severity = "Low", Title = "Crowded 2.4 GHz channels", Description = "The 2.4 GHz band appears crowded relative to available channels." }
        };

        var recommendations = new List<InsightRecommendation>
        {
            new() { Id = "REC-001", Priority = "P1", Title = "Disable or secure open SSIDs", Description = "Require WPA2 or WPA3 on all non-public networks." },
            new() { Id = "REC-002", Priority = "P1", Title = "Retire WEP-capable infrastructure", Description = "Replace obsolete devices and enforce modern encryption profiles." },
            new() { Id = "REC-003", Priority = "P2", Title = "Improve AP placement", Description = "Reposition or add access points to reduce weak coverage areas." },
            new() { Id = "REC-004", Priority = "P3", Title = "Rebalance channel usage", Description = "Prefer 5 GHz and 6 GHz where supported to reduce 2.4 GHz contention." }
        };

        return (issues, recommendations);
    }
}