using Vyre.App.Models;

namespace Vyre.App.Services.Analysis;

public sealed class WifiAnalyzerService : IWifiAnalyzerService
{
    public Task<AnalysisReportModel> AnalyzeAsync(
        IReadOnlyList<AccessPointViewData> accessPoints,
        string sourcePlatform,
        bool isPartial,
        string capabilityMessage,
        IReadOnlyList<string>? warnings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<InsightIssue>();
        var safeWarnings = warnings ?? Array.Empty<string>();

        if (isPartial)
        {
            issues.Add(new InsightIssue
            {
                Rank = 0,
                Code = "PLATFORM_LIMITATION",
                Severity = "Info",
                Title = "Platform-limited visibility",
                Description = "This platform supplied only a partial view of nearby Wi-Fi information.",
                Evidence = capabilityMessage,
                FixSteps = "Use diagnostics together with scan data and avoid over-interpreting missing fields."
            });
        }

        var openCount = accessPoints.Count(x => x.SecurityCategory.Equals("Open", StringComparison.OrdinalIgnoreCase));
        if (openCount > 0)
        {
            issues.Add(new InsightIssue
            {
                Rank = 0,
                Code = "OPEN_NETWORK",
                Severity = "High",
                Title = "Open network detected",
                Description = "At least one visible access point is not protected by encryption.",
                Evidence = $"{openCount} open access point(s) observed.",
                FixSteps = "Prefer WPA2 or WPA3. Avoid sensitive traffic on open Wi-Fi."
            });
        }

        var wepCount = accessPoints.Count(x => x.SecurityCategory.Equals("WEP", StringComparison.OrdinalIgnoreCase));
        if (wepCount > 0)
        {
            issues.Add(new InsightIssue
            {
                Rank = 0,
                Code = "LEGACY_WEP",
                Severity = "High",
                Title = "Legacy WEP security detected",
                Description = "WEP is obsolete and insecure.",
                Evidence = $"{wepCount} WEP access point(s) observed.",
                FixSteps = "Replace WEP with WPA2 or WPA3 immediately."
            });
        }

        var weakCount = accessPoints.Count(x => x.SignalDbm <= -75);
        var veryWeakCount = accessPoints.Count(x => x.SignalDbm <= -82);
        if (weakCount > 0)
        {
            issues.Add(new InsightIssue
            {
                Rank = 0,
                Code = "WEAK_SIGNAL",
                Severity = veryWeakCount > 0 ? "High" : "Medium",
                Title = "Weak signal may affect stability",
                Description = "Some networks show weak RSSI and may behave poorly.",
                Evidence = $"{weakCount} weak AP(s), including {veryWeakCount} very weak AP(s).",
                FixSteps = "Move closer to the AP, improve placement, or add better coverage."
            });
        }

        var channelLoad24 = new Dictionary<int, int>();
        foreach (var ap in accessPoints.Where(x => x.Band == "2.4 GHz" && x.Channel > 0))
        {
            for (var probe = 1; probe <= 14; probe++)
            {
                if (Math.Abs(ap.Channel - probe) < 5)
                {
                    channelLoad24[probe] = channelLoad24.TryGetValue(probe, out var count) ? count + 1 : 1;
                }
            }
        }

        if (channelLoad24.Count > 0)
        {
            var busiest = channelLoad24.OrderByDescending(x => x.Value).First();
            if (busiest.Value >= 4)
            {
                issues.Add(new InsightIssue
                {
                    Rank = 0,
                    Code = "CHANNEL_CROWDING_24",
                    Severity = "Medium",
                    Title = "2.4 GHz channel crowding detected",
                    Description = "Overlapping 2.4 GHz networks may compete for airtime and reduce performance.",
                    Evidence = $"Estimated overlap around channel {busiest.Key} involves {busiest.Value} AP observations.",
                    FixSteps = "Prefer channels 1, 6, or 11. Move capable clients to 5 GHz or 6 GHz."
                });
            }
        }

        foreach (var group in accessPoints
            .Where(x => !string.IsNullOrWhiteSpace(x.Ssid) && x.Ssid != "<Hidden>")
            .GroupBy(x => x.Ssid, StringComparer.OrdinalIgnoreCase))
        {
            var groupList = group.ToList();
            if (groupList.Count < 2)
            {
                continue;
            }

            var securityMismatch = groupList.Select(x => x.Security).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1;
            var vendorMismatch = groupList.Where(x => !string.IsNullOrWhiteSpace(x.Vendor))
                .Select(x => x.Vendor)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() > 1;

            if (securityMismatch || vendorMismatch)
            {
                var evidence = $"SSID \"{group.Key}\" seen on {groupList.Count} BSSID(s)";
                if (securityMismatch)
                {
                    evidence += " with differing security labels";
                }

                if (vendorMismatch)
                {
                    evidence += securityMismatch ? " and differing vendors." : " with differing vendors.";
                }
                else
                {
                    evidence += ".";
                }

                issues.Add(new InsightIssue
                {
                    Rank = 0,
                    Code = "POSSIBLE_SSID_CLONE",
                    Severity = "Low",
                    Title = "Possible SSID clone or inconsistent configuration",
                    Description = "The same SSID was seen with conflicting characteristics. This may be normal in larger deployments but should be verified carefully.",
                    Evidence = evidence,
                    FixSteps = "Verify the expected security profile and vendor inventory for this SSID before treating it as suspicious."
                });
            }
        }

        foreach (var warning in safeWarnings)
        {
            issues.Add(new InsightIssue
            {
                Rank = 0,
                Code = "PLATFORM_WARNING",
                Severity = "Info",
                Title = "Platform warning",
                Description = "The scanner reported a warning that may affect data completeness.",
                Evidence = warning,
                FixSteps = "Re-run the scan after clearing the warning condition."
            });
        }

        if (issues.Count == 0)
        {
            issues.Add(new InsightIssue
            {
                Rank = 0,
                Code = "NO_MAJOR_ISSUES",
                Severity = "Info",
                Title = "No major issues detected",
                Description = "No obvious security or stability problems were detected from the current scan data.",
                Evidence = "Analysis completed without high-confidence findings.",
                FixSteps = "Repeat scans under real conditions to catch intermittent issues."
            });
        }

        var ranked = issues
            .OrderByDescending(x => SeverityWeight(x.Severity))
            .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new InsightIssue
            {
                Rank = index + 1,
                Code = item.Code,
                Severity = item.Severity,
                Title = item.Title,
                Description = item.Description,
                Evidence = item.Evidence,
                FixSteps = item.FixSteps
            })
            .ToList();

        return Task.FromResult(new AnalysisReportModel
        {
            SourcePlatform = sourcePlatform,
            IsPartial = isPartial,
            CapabilityMessage = capabilityMessage,
            Warnings = safeWarnings,
            AccessPoints = accessPoints,
            Issues = ranked
        });
    }

    private static int SeverityWeight(string severity) =>
        severity switch
        {
            "Critical" => 4,
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => 0
        };
}