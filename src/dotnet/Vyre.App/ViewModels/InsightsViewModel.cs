using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyre.App.Models;
using Vyre.App.Services.Wifi;

namespace Vyre.App.ViewModels;

public sealed partial class InsightsViewModel : BaseViewModel
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    private readonly IWifiScanService _wifiScanService;

    [ObservableProperty] private string summaryMessage = "Run a scan to build RF, security, and channel intelligence.";
    [ObservableProperty] private string healthScoreText = "--";
    [ObservableProperty] private string postureLabel = "No scan loaded";
    [ObservableProperty] private string postureDetail = "Insights are generated from the latest scan, not static sample text.";
    [ObservableProperty] private string lastScanText = "No scan loaded";
    [ObservableProperty] private bool hasNoScan = true;
    [ObservableProperty] private bool hasInsights;
    [ObservableProperty] private int criticalOrHighCount;
    [ObservableProperty] private int recommendationCount;

    public ObservableCollection<InsightMetricCard> OverviewCards { get; } = new();
    public ObservableCollection<InsightIssue> Issues { get; } = new();
    public ObservableCollection<InsightRecommendation> Recommendations { get; } = new();

    public IAsyncRelayCommand RefreshCommand { get; }

    public InsightsViewModel(IWifiScanService wifiScanService)
    {
        _wifiScanService = wifiScanService;
        Title = "Insights";
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task InitializeAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            using var cts = new CancellationTokenSource();
            var snapshot = await _wifiScanService.GetLatestAsync(cts.Token);

            if (snapshot is null)
            {
                await MainThread.InvokeOnMainThreadAsync(ApplyNoScanState);
                return;
            }

            var issues = BuildIssueSet(snapshot);
            var recommendations = BuildRecommendationSet(snapshot, issues);
            var cards = BuildOverviewCards(snapshot, issues);
            var score = CalculateScore(snapshot, issues);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HasNoScan = false;
                HasInsights = true;
                HealthScoreText = string.Create(InvariantCulture, $"{score}/100");
                PostureLabel = BuildPostureLabel(score, issues);
                PostureDetail = BuildPostureDetail(snapshot, issues);
                LastScanText = string.Create(InvariantCulture, $"Last scan {snapshot.CapturedAtUtc:HH:mm:ss} UTC");
                SummaryMessage = BuildSummary(snapshot, issues);
                CriticalOrHighCount = issues.Count(x => IsHighImpact(x.Severity));
                RecommendationCount = recommendations.Count;

                OverviewCards.Clear();
                foreach (var card in cards)
                {
                    OverviewCards.Add(card);
                }

                Issues.Clear();
                foreach (var issue in issues)
                {
                    Issues.Add(issue);
                }

                Recommendations.Clear();
                foreach (var recommendation in recommendations)
                {
                    Recommendations.Add(recommendation);
                }
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load insights: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyNoScanState()
    {
        HasNoScan = true;
        HasInsights = false;
        SummaryMessage = "No scan results yet. Run a scan first.";
        HealthScoreText = "--";
        PostureLabel = "No scan loaded";
        PostureDetail = "Run a scan to build RF, security, channel, and identity findings.";
        LastScanText = "No scan loaded";
        CriticalOrHighCount = 0;
        RecommendationCount = 0;
        OverviewCards.Clear();
        Issues.Clear();
        Recommendations.Clear();
    }

    private static List<InsightIssue> BuildIssueSet(ScanInsightSnapshot snapshot)
    {
        var issues = new List<InsightIssue>();

        foreach (var issue in BuildDerivedIssues(snapshot))
        {
            AddIssue(issues, issue);
        }

        foreach (var issue in snapshot.Issues ?? Array.Empty<InsightIssue>())
        {
            AddIssue(issues, NormalizeIssue(issue));
        }

        return RankIssues(issues);
    }

    private static IEnumerable<InsightIssue> BuildDerivedIssues(ScanInsightSnapshot snapshot)
    {
        var aps = snapshot.AccessPoints.ToList();

        if (aps.Count == 0)
        {
            yield return NewIssue(
                "NO_OBSERVATIONS",
                "Medium",
                "Data quality",
                "No access points were observed",
                "The latest scan produced no AP records, so Vyre cannot build a useful local RF profile.",
                "The scan payload contained zero access points.",
                "Verify Wi-Fi is enabled, permissions are granted, and the device is not rate-limited by the OS.");
            yield break;
        }

        if (snapshot.IsPartial)
        {
            yield return NewIssue(
                "PARTIAL_VISIBILITY",
                "Medium",
                "Data quality",
                "Platform-limited scan visibility",
                "The platform returned a partial view. Treat missing networks, vendor fields, and security metadata as unknown rather than safe.",
                Safe(snapshot.CapabilityMessage, "The scanner marked this result as partial."),
                "Use repeated scans and Doctor diagnostics to separate OS limits from real RF conditions.");
        }

        var open = aps.Where(IsOpenNetwork).ToList();
        if (open.Count > 0)
        {
            yield return NewIssue(
                "OPEN_NETWORK_EXPOSURE",
                "High",
                "Security",
                "Open SSID exposure is visible",
                "At least one nearby network is unencrypted. This is not just a password issue; passive observers can inspect or manipulate traffic around clients that trust it.",
                string.Create(InvariantCulture, $"{open.Count} open AP observation(s): {JoinNames(open)}"),
                "Prefer WPA2/WPA3. If open access is intentional, isolate it as guest-only and do not allow LAN reachability.");
        }

        var wep = aps.Where(x => ContainsSecurity(x, "WEP")).ToList();
        if (wep.Count > 0)
        {
            yield return NewIssue(
                "WEP_LEGACY_CRYPTO",
                "High",
                "Security",
                "Legacy WEP is present",
                "WEP can be broken quickly and should be treated as equivalent to open Wi-Fi for trust decisions.",
                string.Create(InvariantCulture, $"{wep.Count} WEP AP observation(s): {JoinNames(wep)}"),
                "Retire WEP SSIDs. Replace with WPA2-Personal or WPA3-SAE depending on device compatibility.");
        }

        var unknownSecurity = aps.Where(HasUnknownSecurity).ToList();
        if (unknownSecurity.Count > 0)
        {
            yield return NewIssue(
                "UNKNOWN_SECURITY_LABELS",
                "Low",
                "Security",
                "Some security labels are incomplete",
                "The scanner could not confidently classify every AP. Unknown does not mean unsafe, but it should not be counted as verified protection.",
                string.Create(InvariantCulture, $"{unknownSecurity.Count} AP(s) had unknown or blank security labels."),
                "Validate router configuration directly for important SSIDs and repeat the scan near the access point.");
        }

        var signals = aps.Where(x => x.SignalDbm != 0).Select(x => x.SignalDbm).ToList();
        if (signals.Count > 0)
        {
            var weak = aps.Where(x => x.SignalDbm != 0 && x.SignalDbm <= -75).ToList();
            if (weak.Count > 0)
            {
                var veryWeak = weak.Count(x => x.SignalDbm <= -82);
                yield return NewIssue(
                    "EDGE_COVERAGE_RISK",
                    veryWeak > 0 ? "High" : "Medium",
                    "RF coverage",
                    "Weak edge signal detected",
                    "Low RSSI can cause retries, roaming instability, and misleading speed-test results even when the internet connection is healthy.",
                    string.Create(InvariantCulture, $"{weak.Count} weak AP(s), {veryWeak} very weak AP(s). Weakest observation: {signals.Min()} dBm."),
                    "Move the AP, add coverage, or test client roaming at the location where the weak observation was captured.");
            }

            var spread = signals.Max() - signals.Min();
            if (spread >= 28 && aps.Count > 1)
            {
                yield return NewIssue(
                    "RF_POWER_IMBALANCE",
                    "Low",
                    "RF coverage",
                    "Large signal spread across visible APs",
                    "A wide RSSI spread can indicate uneven AP placement or a scan position that favors one AP while clients at the edge may roam poorly.",
                    string.Create(InvariantCulture, $"Observed spread: {spread} dB, from {signals.Min()} dBm to {signals.Max()} dBm."),
                    "Repeat scans from normal work areas and compare whether the dominant AP changes cleanly.");
            }
        }

        var twoFour = aps.Where(x => IsBand(x, "2.4")).ToList();
        var nonStandard24 = twoFour.Where(x => x.Channel > 0 && x.Channel is not 1 and not 6 and not 11).ToList();
        if (nonStandard24.Count > 0)
        {
            yield return NewIssue(
                "OVERLAPPING_24GHZ_CHANNELS",
                "Medium",
                "Channel plan",
                "2.4 GHz channels may overlap",
                "2.4 GHz channels outside 1, 6, and 11 tend to overlap neighboring cells and create avoidable airtime contention.",
                string.Create(InvariantCulture, $"{nonStandard24.Count} AP(s) were observed on overlapping 2.4 GHz channels: {JoinChannels(nonStandard24)}."),
                "Use channels 1, 6, or 11 for 20 MHz 2.4 GHz cells. Avoid 40 MHz 2.4 GHz unless the environment is isolated.");
        }

        if (twoFour.Count > 0 && aps.All(x => !IsBand(x, "5") && !IsBand(x, "6")))
        {
            yield return NewIssue(
                "ONLY_24GHZ_VISIBLE",
                "Low",
                "Channel plan",
                "Only 2.4 GHz is visible",
                "A 2.4-only view is more vulnerable to interference and lower throughput. It may be expected on old hardware, but it is a constraint worth calling out.",
                string.Create(InvariantCulture, $"{twoFour.Count} AP observation(s), zero 5 GHz or 6 GHz observations."),
                "Enable 5 GHz or 6 GHz where hardware supports it, or document that this area is intentionally legacy-only.");
        }

        var ssidGroups = aps
            .Where(x => !string.IsNullOrWhiteSpace(x.Ssid) && !IsHiddenSsid(x))
            .GroupBy(x => x.Ssid.Trim(), StringComparer.OrdinalIgnoreCase);

        foreach (var group in ssidGroups)
        {
            var groupItems = group.ToList();
            if (groupItems.Count < 2)
            {
                continue;
            }

            var securityMismatch = groupItems.Select(SecurityText).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1;
            var vendorMismatch = groupItems
                .Where(x => !string.IsNullOrWhiteSpace(x.Vendor))
                .Select(x => x.Vendor.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() > 1;

            if (securityMismatch || vendorMismatch)
            {
                yield return NewIssue(
                    string.Create(InvariantCulture, $"SSID_PROFILE_DRIFT_{StableCode(group.Key)}"),
                    securityMismatch ? "Medium" : "Low",
                    "Identity",
                    string.Create(InvariantCulture, $"SSID profile drift: {group.Key}"),
                    "The same SSID appears with differing characteristics. That can be normal in enterprise networks, but it is also how misconfigured extenders and evil-twin tests appear.",
                    string.Create(InvariantCulture, $"{groupItems.Count} BSSID(s). Security mismatch: {securityMismatch}. Vendor mismatch: {vendorMismatch}."),
                    "Verify each BSSID belongs to your inventory and uses the expected security profile.");
            }
        }

        var hidden = aps.Where(IsHiddenSsid).ToList();
        if (hidden.Count > 0)
        {
            yield return NewIssue(
                "HIDDEN_SSID_PRESENT",
                "Low",
                "Identity",
                "Hidden SSID observations are present",
                "Hidden SSIDs do not provide meaningful secrecy. They can make client behavior noisier and harder to audit.",
                string.Create(InvariantCulture, $"{hidden.Count} hidden or blank SSID observation(s)."),
                "Prefer explicit SSID naming and rely on strong authentication instead of hiding broadcast names.");
        }

        var unknownVendor = aps.Where(x => string.IsNullOrWhiteSpace(x.Vendor) || x.Vendor.Equals("Unknown", StringComparison.OrdinalIgnoreCase)).ToList();
        if (unknownVendor.Count > 0)
        {
            yield return NewIssue(
                "VENDOR_ATTRIBUTION_GAP",
                unknownVendor.Count == aps.Count ? "Medium" : "Low",
                "Identity",
                "Vendor attribution is incomplete",
                "Missing vendor names reduce confidence when deciding whether an AP belongs to the expected environment.",
                string.Create(InvariantCulture, $"{unknownVendor.Count} of {aps.Count} AP(s) had no resolved vendor."),
                "Refresh the OUI database, validate BSSIDs against router inventory, and flag unknown vendors in sensitive locations.");
        }

        foreach (var warning in snapshot.Warnings)
        {
            yield return NewIssue(
                string.Create(InvariantCulture, $"PLATFORM_WARNING_{StableCode(warning)}"),
                "Low",
                "Platform",
                "Platform warning affects interpretation",
                "The scanner reported a warning that may change how complete or fresh the observations are.",
                warning,
                "Resolve the platform condition, then scan again before making network decisions.");
        }
    }

    private static List<InsightRecommendation> BuildRecommendationSet(
        ScanInsightSnapshot snapshot,
        List<InsightIssue> issues)
    {
        var recommendations = new List<InsightRecommendation>();
        var snapshotRecommendations = (snapshot.Recommendations ?? Array.Empty<InsightRecommendation>())
            .Select(NormalizeRecommendation)
            .ToList();

        var aps = snapshot.AccessPoints.ToList();

        if (aps.Count == 0)
        {
            AddRecommendation(
                recommendations,
                NewRecommendation(
                    "REC-SCAN-QUALITY",
                    "P1",
                    "Data quality",
                    "Recover usable scan data",
                    "A scan with no AP records cannot support RF decisions.",
                    "Check OS permissions, Wi-Fi state, and platform scanner availability, then scan again.",
                    "No AP records were available.",
                    "Restores the basic evidence needed for all other recommendations.",
                    "Low"));

            foreach (var recommendation in snapshotRecommendations)
            {
                AddRecommendation(recommendations, recommendation);
            }

            return SortRecommendations(recommendations);
        }

        if (issues.Any(x => x.Code.Contains("OPEN", StringComparison.OrdinalIgnoreCase) || x.Code.Contains("WEP", StringComparison.OrdinalIgnoreCase)))
        {
            AddRecommendation(
                recommendations,
                NewRecommendation(
                    "REC-SEC-SEGMENT-OPEN",
                    "P1",
                    "Security architecture",
                    "Treat open or WEP networks as untrusted zones",
                    "Open/WEP networks should not share trust with private devices, admin panels, printers, or storage.",
                    "Move them to a guest VLAN, block client-to-LAN access, and enforce WPA2/WPA3 for private SSIDs.",
                    "The scan observed unencrypted or legacy-protected APs.",
                    "Reduces credential exposure and lateral movement risk.",
                    "Medium"));
        }

        if (issues.Any(x => x.Code.Contains("CHANNEL", StringComparison.OrdinalIgnoreCase)))
        {
            AddRecommendation(
                recommendations,
                NewRecommendation(
                    "REC-RF-CHANNEL-PLAN",
                    "P2",
                    "RF engineering",
                    "Rebuild the 2.4 GHz channel plan",
                    "Overlapping 2.4 GHz channels waste airtime even when signal bars look strong.",
                    "Set 2.4 GHz cells to 20 MHz on channels 1, 6, or 11, then rescan from the same position.",
                    "The channel analysis found overlap-prone observations.",
                    "Improves reliability for low-power and legacy clients.",
                    "Medium"));
        }

        if (issues.Any(x => x.Code.Contains("EDGE_COVERAGE", StringComparison.OrdinalIgnoreCase) || x.Code.Contains("RF_POWER", StringComparison.OrdinalIgnoreCase)))
        {
            AddRecommendation(
                recommendations,
                NewRecommendation(
                    "REC-RF-WALK-TEST",
                    "P2",
                    "RF validation",
                    "Run a small walk-test baseline",
                    "One scan point cannot separate real dead zones from one unlucky scan location.",
                    "Capture scans at the router, normal work area, and far edge. Compare signal deltas and report archives.",
                    "Weak or uneven RSSI was detected.",
                    "Turns guesswork into repeatable coverage evidence.",
                    "Low"));
        }

        if (issues.Any(x => x.Code.Contains("SSID_PROFILE", StringComparison.OrdinalIgnoreCase) || x.Code.Contains("VENDOR", StringComparison.OrdinalIgnoreCase)))
        {
            AddRecommendation(
                recommendations,
                NewRecommendation(
                    "REC-ID-INVENTORY",
                    "P2",
                    "Identity control",
                    "Build a BSSID allowlist for trusted environments",
                    "SSID names are not identity. BSSID and vendor consistency are better signals for spotting drift.",
                    "Record expected BSSIDs/vendors for known routers and flag future scans that introduce unknown hardware.",
                    "The scan found identity drift or unresolved vendors.",
                    "Improves evil-twin and misconfiguration detection.",
                    "Medium"));
        }

        if (aps.Any(x => IsBand(x, "2.4")) && aps.All(x => !IsBand(x, "5") && !IsBand(x, "6")))
        {
            AddRecommendation(
                recommendations,
                NewRecommendation(
                    "REC-BAND-STEERING",
                    "P3",
                    "Capacity planning",
                    "Prefer 5 GHz or 6 GHz for capable devices",
                    "2.4 GHz should be the compatibility band, not the primary lane for modern clients.",
                    "Enable 5 GHz/6 GHz SSIDs where hardware permits and move high-throughput clients off 2.4 GHz.",
                    "Only 2.4 GHz AP observations were visible.",
                    "Reduces contention and improves throughput headroom.",
                    "Medium"));
        }

        if (snapshot.Warnings.Count > 0 || snapshot.IsPartial)
        {
            AddRecommendation(
                recommendations,
                NewRecommendation(
                    "REC-PLATFORM-CONTEXT",
                    "P3",
                    "Evidence quality",
                    "Label reports with platform limits",
                    "Android, Windows, and iOS expose different Wi-Fi details. Comparing them as identical data sources creates false confidence.",
                    "Keep the platform and warning text attached to each saved report, especially before comparing scans.",
                    Safe(snapshot.CapabilityMessage, "The scan included platform warnings or partial visibility."),
                    "Prevents over-interpreting missing fields as clean results.",
                    "Low"));
        }

        AddRecommendation(
            recommendations,
            NewRecommendation(
                "REC-BASELINE-REPORTS",
                "P3",
                "Operational workflow",
                "Use report history as a before-and-after baseline",
                "The useful question is not only what is wrong now, but what changed after a router, channel, or placement change.",
                "Save a report before changes, make one network change, then scan again and compare counts, security labels, channels, and signal.",
                string.Create(InvariantCulture, $"Current scan has {aps.Count} AP(s) and {issues.Count} finding(s)."),
                "Makes tuning work measurable instead of anecdotal.",
                "Low"));

        foreach (var recommendation in snapshotRecommendations)
        {
            AddRecommendation(recommendations, recommendation);
        }

        return SortRecommendations(recommendations);
    }

    private static List<InsightMetricCard> BuildOverviewCards(
        ScanInsightSnapshot snapshot,
        IReadOnlyList<InsightIssue> issues)
    {
        var aps = snapshot.AccessPoints.ToList();
        var highImpact = issues.Count(x => IsHighImpact(x.Severity));
        var signals = aps.Where(x => x.SignalDbm != 0).Select(x => x.SignalDbm).ToList();
        var openOrLegacy = aps.Count(x => IsOpenNetwork(x) || ContainsSecurity(x, "WEP"));
        var bandSummary = BuildBandSummary(aps);

        var cards = new List<InsightMetricCard>
        {
            new()
            {
                Label = "Risk posture",
                Value = highImpact == 0 ? "Controlled" : string.Create(InvariantCulture, $"{highImpact} urgent"),
                Detail = highImpact == 0 ? "No high-impact findings in the current scan." : "High/Critical findings need action before trusting this environment.",
                AccentHex = highImpact == 0 ? "#22C55E" : "#EF4444"
            },
            new()
            {
                Label = "Exposure",
                Value = openOrLegacy == 0 ? "Encrypted" : string.Create(InvariantCulture, $"{openOrLegacy} exposed"),
                Detail = openOrLegacy == 0 ? "No open or WEP observations found." : "Open or legacy-protected networks were visible.",
                AccentHex = openOrLegacy == 0 ? "#22C55E" : "#F59E0B"
            },
            new()
            {
                Label = "RF floor",
                Value = signals.Count == 0 ? "Unknown" : string.Create(InvariantCulture, $"{signals.Min()} dBm"),
                Detail = signals.Count == 0 ? "Signal was not available from this platform." : string.Create(InvariantCulture, $"Best {signals.Max()} dBm across {signals.Count} signal observation(s)."),
                AccentHex = signals.Count == 0 || signals.Min() <= -75 ? "#F59E0B" : "#38BDF8"
            },
            new()
            {
                Label = "Band mix",
                Value = bandSummary.Value,
                Detail = bandSummary.Detail,
                AccentHex = bandSummary.Accent
            }
        };

        return cards;
    }

    private static int CalculateScore(ScanInsightSnapshot snapshot, IReadOnlyList<InsightIssue> issues)
    {
        var score = 100;

        foreach (var issue in issues)
        {
            score -= issue.Severity switch
            {
                "Critical" => 24,
                "High" => 18,
                "Medium" => 10,
                "Low" => 4,
                _ => 1
            };
        }

        if (snapshot.IsPartial)
        {
            score -= 8;
        }

        score -= Math.Min(snapshot.Warnings.Count * 3, 12);
        return Math.Clamp(score, 0, 100);
    }

    private static string BuildSummary(ScanInsightSnapshot snapshot, IReadOnlyList<InsightIssue> issues)
    {
        var urgent = issues.Count(x => IsHighImpact(x.Severity));
        var suffix = snapshot.Warnings.Count > 0
            ? string.Create(InvariantCulture, $" / {snapshot.Warnings.Count} warning(s)")
            : string.Empty;

        return string.Create(
            InvariantCulture,
            $"{snapshot.SourcePlatform} / {snapshot.AccessPoints.Count} AP(s) / {urgent} urgent finding(s){suffix}");
    }

    private static string BuildPostureLabel(int score, IReadOnlyList<InsightIssue> issues)
    {
        if (issues.Any(x => x.Severity == "Critical" || x.Severity == "High"))
        {
            return "Action required";
        }

        return score switch
        {
            >= 90 => "Clean operating window",
            >= 75 => "Watchlist posture",
            >= 55 => "Needs tuning",
            _ => "Unreliable environment"
        };
    }

    private static string BuildPostureDetail(ScanInsightSnapshot snapshot, List<InsightIssue> issues)
    {
        if (issues.Count == 0)
        {
            return "No findings were generated from the latest scan.";
        }

        var first = issues[0];
        return string.Create(
            InvariantCulture,
            $"Top finding: {first.Title}. Based on {snapshot.AccessPoints.Count} AP observation(s) from {snapshot.SourcePlatform}.");
    }

    private static void AddIssue(List<InsightIssue> issues, InsightIssue issue)
    {
        if (issues.Any(existing => IsDuplicateIssue(existing, issue)))
        {
            return;
        }

        issues.Add(issue);
    }

    private static bool IsDuplicateIssue(InsightIssue existing, InsightIssue candidate)
    {
        if (string.Equals(IssueKey(existing), IssueKey(candidate), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var existingText = string.Concat(existing.Code, " ", existing.Title);
        var candidateText = string.Concat(candidate.Code, " ", candidate.Title);

        return IsSemanticMatch(existingText, candidateText, "OPEN") ||
               IsSemanticMatch(existingText, candidateText, "WEP") ||
               IsSemanticMatch(existingText, candidateText, "WEAK") ||
               IsSemanticMatch(existingText, candidateText, "SIGNAL") ||
               IsSemanticMatch(existingText, candidateText, "PLATFORM_WARNING") ||
               IsSemanticMatch(existingText, candidateText, "PLATFORM WARNING");
    }

    private static bool IsSemanticMatch(string left, string right, string token) =>
        left.Contains(token, StringComparison.OrdinalIgnoreCase) &&
        right.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static void AddRecommendation(List<InsightRecommendation> recommendations, InsightRecommendation recommendation)
    {
        if (recommendations.Any(existing => IsDuplicateRecommendation(existing, recommendation)))
        {
            return;
        }

        recommendations.Add(recommendation);
    }

    private static bool IsDuplicateRecommendation(InsightRecommendation existing, InsightRecommendation candidate)
    {
        if (string.Equals(RecommendationKey(existing), RecommendationKey(candidate), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var existingText = RecommendationText(existing);
        var candidateText = RecommendationText(candidate);

        return IsSemanticMatch(existingText, candidateText, "OPEN") ||
               IsSemanticMatch(existingText, candidateText, "WEP") ||
               IsSemanticMatch(existingText, candidateText, "CHANNEL") ||
               IsSemanticMatch(existingText, candidateText, "BSSID") ||
               IsSemanticMatch(existingText, candidateText, "VENDOR") ||
               IsSemanticMatch(existingText, candidateText, "PLATFORM") ||
               IsSemanticMatch(existingText, candidateText, "WARNING") ||
               IsSemanticMatch(existingText, candidateText, "PARTIAL");
    }

    private static string IssueKey(InsightIssue issue) =>
        string.IsNullOrWhiteSpace(issue.Code) ? issue.Title : issue.Code;

    private static string RecommendationKey(InsightRecommendation recommendation) =>
        string.IsNullOrWhiteSpace(recommendation.Id) ? recommendation.Title : recommendation.Id;

    private static string RecommendationText(InsightRecommendation recommendation) =>
        string.Concat(
            recommendation.Id,
            " ",
            recommendation.Category,
            " ",
            recommendation.Title,
            " ",
            recommendation.Description,
            " ",
            recommendation.Action,
            " ",
            recommendation.Evidence);

    private static List<InsightIssue> RankIssues(List<InsightIssue> issues) =>
        issues
            .OrderByDescending(x => SeverityWeight(x.Severity))
            .ThenBy(x => x.CategoryLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .Select((issue, index) => new InsightIssue
            {
                Rank = index + 1,
                Code = issue.Code,
                Severity = Safe(issue.Severity, "Info"),
                Category = Safe(issue.Category, "General"),
                Title = issue.Title,
                Description = issue.Description,
                Evidence = issue.Evidence,
                FixSteps = issue.FixSteps
            })
            .ToList();

    private static List<InsightRecommendation> SortRecommendations(List<InsightRecommendation> recommendations) =>
        recommendations
            .OrderBy(x => PriorityWeight(x.Priority))
            .ThenBy(x => x.CategoryLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static InsightIssue NormalizeIssue(InsightIssue issue) =>
        new()
        {
            Code = issue.Code,
            Severity = Safe(issue.Severity, "Info"),
            Category = Safe(issue.Category, InferCategory(issue.Code, issue.Title)),
            Title = Safe(issue.Title, "Network finding"),
            Description = Safe(issue.Description, "The latest scan produced a finding that needs review."),
            Evidence = issue.Evidence,
            FixSteps = issue.FixSteps
        };

    private static InsightRecommendation NormalizeRecommendation(InsightRecommendation recommendation) =>
        new()
        {
            Id = recommendation.Id,
            Priority = Safe(recommendation.Priority, "P3"),
            Category = Safe(recommendation.Category, "Scanner"),
            Title = Safe(recommendation.Title, "Review scan result"),
            Description = Safe(recommendation.Description, "Review the current scan context and repeat after changes."),
            Action = string.IsNullOrWhiteSpace(recommendation.Action)
                ? Safe(recommendation.Description, "Review the current scan context and repeat after changes.")
                : recommendation.Action,
            Evidence = recommendation.Evidence,
            Impact = recommendation.Impact,
            Effort = recommendation.Effort
        };

    private static InsightIssue NewIssue(
        string code,
        string severity,
        string category,
        string title,
        string description,
        string evidence,
        string fixSteps) =>
        new()
        {
            Code = code,
            Severity = severity,
            Category = category,
            Title = title,
            Description = description,
            Evidence = evidence,
            FixSteps = fixSteps
        };

    private static InsightRecommendation NewRecommendation(
        string id,
        string priority,
        string category,
        string title,
        string description,
        string action,
        string evidence,
        string impact,
        string effort) =>
        new()
        {
            Id = id,
            Priority = priority,
            Category = category,
            Title = title,
            Description = description,
            Action = action,
            Evidence = evidence,
            Impact = impact,
            Effort = effort
        };

    private static string InferCategory(string code, string title)
    {
        var text = string.Concat(code, " ", title);

        if (text.Contains("SEC", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("OPEN", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("WEP", StringComparison.OrdinalIgnoreCase))
        {
            return "Security";
        }

        if (text.Contains("SIGNAL", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("RF", StringComparison.OrdinalIgnoreCase))
        {
            return "RF coverage";
        }

        if (text.Contains("CHANNEL", StringComparison.OrdinalIgnoreCase))
        {
            return "Channel plan";
        }

        if (text.Contains("SSID", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("VENDOR", StringComparison.OrdinalIgnoreCase))
        {
            return "Identity";
        }

        return "General";
    }

    private static (string Value, string Detail, string Accent) BuildBandSummary(List<AccessPointViewData> aps)
    {
        if (aps.Count == 0)
        {
            return ("None", "No AP observations available.", "#64748B");
        }

        var count24 = aps.Count(x => IsBand(x, "2.4"));
        var count5 = aps.Count(x => IsBand(x, "5"));
        var count6 = aps.Count(x => IsBand(x, "6"));

        var value = string.Create(InvariantCulture, $"{count24}/{count5}/{count6}");
        var detail = string.Create(InvariantCulture, $"2.4 / 5 / 6 GHz observations across {aps.Count} AP(s).");
        var accent = count5 + count6 > 0 ? "#38BDF8" : "#F59E0B";
        return (value, detail, accent);
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

    private static int PriorityWeight(string priority) =>
        priority switch
        {
            "P0" => 0,
            "P1" => 1,
            "P2" => 2,
            _ => 3
        };

    private static bool IsHighImpact(string severity) => severity is "Critical" or "High";

    private static bool IsOpenNetwork(AccessPointViewData accessPoint) =>
        accessPoint.IsOpen || SecurityText(accessPoint).Equals("Open", StringComparison.OrdinalIgnoreCase);

    private static bool HasUnknownSecurity(AccessPointViewData accessPoint)
    {
        var security = SecurityText(accessPoint);
        return string.IsNullOrWhiteSpace(security) ||
               security.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsSecurity(AccessPointViewData accessPoint, string value) =>
        SecurityText(accessPoint).Contains(value, StringComparison.OrdinalIgnoreCase);

    private static string SecurityText(AccessPointViewData accessPoint) =>
        string.IsNullOrWhiteSpace(accessPoint.SecurityCategory)
            ? accessPoint.Security
            : accessPoint.SecurityCategory;

    private static bool IsBand(AccessPointViewData accessPoint, string marker) =>
        accessPoint.Band.Contains(marker, StringComparison.OrdinalIgnoreCase);

    private static bool IsHiddenSsid(AccessPointViewData accessPoint)
    {
        var ssid = accessPoint.Ssid.Trim();
        return string.IsNullOrWhiteSpace(ssid) ||
               ssid.Equals("<Hidden>", StringComparison.OrdinalIgnoreCase) ||
               ssid.Equals("Hidden", StringComparison.OrdinalIgnoreCase);
    }

    private static string JoinNames(IReadOnlyList<AccessPointViewData> accessPoints) =>
        string.Join(
            ", ",
            accessPoints
                .Take(3)
                .Select(x => string.IsNullOrWhiteSpace(x.Ssid) ? x.Bssid : x.Ssid));

    private static string JoinChannels(IReadOnlyList<AccessPointViewData> accessPoints) =>
        string.Join(
            ", ",
            accessPoints
                .Where(x => x.Channel > 0)
                .Select(x => x.Channel.ToString(InvariantCulture))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Take(6));

    private static string StableCode(string value)
    {
        var chars = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .Take(18)
            .ToArray();

        return chars.Length == 0 ? "UNKNOWN" : new string(chars);
    }

    private static string Safe(string? value, string fallback = "Unknown") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
