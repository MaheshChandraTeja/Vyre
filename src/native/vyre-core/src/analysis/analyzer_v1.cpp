#include "vyre/core/analysis.hpp"

#include <algorithm>
#include <cmath>
#include <iomanip>
#include <map>
#include <sstream>
#include <unordered_map>

namespace vyre::core::analysis
{
    namespace
    {
        int SeverityWeight(const std::string_view severity)
        {
            if (severity == "Critical") return 4;
            if (severity == "High") return 3;
            if (severity == "Medium") return 2;
            if (severity == "Low") return 1;
            return 0;
        }

        std::string JsonEscape(const std::string_view value)
        {
            std::ostringstream out;

            for (const auto ch : value)
            {
                switch (ch)
                {
                case '\\': out << "\\\\"; break;
                case '"': out << "\\\""; break;
                case '\b': out << "\\b"; break;
                case '\f': out << "\\f"; break;
                case '\n': out << "\\n"; break;
                case '\r': out << "\\r"; break;
                case '\t': out << "\\t"; break;
                default:
                    if (static_cast<unsigned char>(ch) < 0x20)
                    {
                        out << "\\u"
                            << std::hex
                            << std::setw(4)
                            << std::setfill('0')
                            << static_cast<int>(static_cast<unsigned char>(ch))
                            << std::dec;
                    }
                    else
                    {
                        out << ch;
                    }
                    break;
                }
            }

            return out.str();
        }

        bool IsWeakSignal(const NormalizedAccessPoint& ap)
        {
            return ap.signal_dbm <= -75;
        }

        bool IsVeryWeakSignal(const NormalizedAccessPoint& ap)
        {
            return ap.signal_dbm <= -82;
        }

        bool Overlaps24Ghz(const int left, const int right)
        {
            if (left <= 0 || right <= 0)
            {
                return false;
            }

            return std::abs(left - right) < 5;
        }

        void AppendIssue(
            std::vector<AnalysisIssue>& issues,
            std::string code,
            std::string severity,
            std::string title,
            std::string description,
            std::string evidence,
            std::string fix_steps)
        {
            issues.push_back(AnalysisIssue{
                .code = std::move(code),
                .severity = std::move(severity),
                .title = std::move(title),
                .description = std::move(description),
                .evidence = std::move(evidence),
                .fix_steps = std::move(fix_steps),
                .rank = 0
            });
        }
    }

    AnalysisReport AnalyzeNormalizedAccessPoints(
        const std::string_view source_platform,
        const bool is_partial,
        const std::string_view capability_message,
        const std::vector<std::string>& warnings,
        const std::vector<NormalizedAccessPoint>& access_points)
    {
        AnalysisReport report;
        report.source_platform = std::string(source_platform);
        report.is_partial = is_partial;
        report.capability_message = std::string(capability_message);
        report.warnings = warnings;
        report.access_points = access_points;

        if (is_partial)
        {
            AppendIssue(
                report.issues,
                "PLATFORM_LIMITATION",
                "Info",
                "Platform-limited visibility",
                "This platform supplied a partial view of nearby networks.",
                std::string(capability_message),
                "Use the Doctor screen and diagnostics alongside scan data. Do not over-interpret missing AP fields on this platform.");
        }

        const auto open_count = static_cast<int>(std::count_if(
            access_points.begin(),
            access_points.end(),
            [](const NormalizedAccessPoint& ap) { return ap.security_category == SecurityCategory::Open; }));

        if (open_count > 0)
        {
            AppendIssue(
                report.issues,
                "OPEN_NETWORK",
                "High",
                "Open network detected",
                "At least one visible access point is not protected by encryption.",
                std::to_string(open_count) + " open access point(s) observed.",
                "Prefer WPA2 or WPA3. Avoid sending sensitive traffic over open Wi-Fi.");
        }

        const auto wep_count = static_cast<int>(std::count_if(
            access_points.begin(),
            access_points.end(),
            [](const NormalizedAccessPoint& ap) { return ap.security_category == SecurityCategory::Wep; }));

        if (wep_count > 0)
        {
            AppendIssue(
                report.issues,
                "LEGACY_WEP",
                "High",
                "Legacy WEP security detected",
                "WEP is obsolete and should be considered insecure.",
                std::to_string(wep_count) + " WEP access point(s) observed.",
                "Replace WEP with WPA2 or WPA3 immediately.");
        }

        const auto weak_count = static_cast<int>(std::count_if(
            access_points.begin(),
            access_points.end(),
            IsWeakSignal));

        const auto very_weak_count = static_cast<int>(std::count_if(
            access_points.begin(),
            access_points.end(),
            IsVeryWeakSignal));

        if (weak_count > 0)
        {
            AppendIssue(
                report.issues,
                "WEAK_SIGNAL",
                very_weak_count > 0 ? "High" : "Medium",
                "Weak signal may affect stability",
                "Some networks show weak RSSI and may experience roaming, retries, or unstable throughput.",
                std::to_string(weak_count) + " weak access point(s), including " + std::to_string(very_weak_count) + " very weak signal(s).",
                "Move closer to the AP, improve placement, or add coverage where needed.");
        }

        std::unordered_map<int, int> channel_load_24;
        for (const auto& ap : access_points)
        {
            if (ap.band == "2.4 GHz" && ap.channel > 0)
            {
                for (int probe = 1; probe <= 14; ++probe)
                {
                    if (Overlaps24Ghz(ap.channel, probe))
                    {
                        channel_load_24[probe]++;
                    }
                }
            }
        }

        const auto max_loaded_24 = std::max_element(
            channel_load_24.begin(),
            channel_load_24.end(),
            [](const auto& left, const auto& right)
            {
                return left.second < right.second;
            });

        if (max_loaded_24 != channel_load_24.end() && max_loaded_24->second >= 4)
        {
            AppendIssue(
                report.issues,
                "CHANNEL_CROWDING_24",
                "Medium",
                "2.4 GHz channel crowding detected",
                "Multiple overlapping 2.4 GHz networks may compete for airtime and reduce performance.",
                "Estimated overlap around channel " + std::to_string(max_loaded_24->first) + " involves " + std::to_string(max_loaded_24->second) + " overlapping AP observations.",
                "Prefer channels 1, 6, or 11 where possible. Move capable clients to 5 GHz or 6 GHz.");
        }

        std::unordered_map<std::string, std::vector<const NormalizedAccessPoint*>> by_ssid;
        for (const auto& ap : access_points)
        {
            if (!ap.ssid.empty() && ap.ssid != "<Hidden>")
            {
                by_ssid[ap.ssid].push_back(&ap);
            }
        }

        for (const auto& [ssid, group] : by_ssid)
        {
            if (group.size() < 2)
            {
                continue;
            }

            bool security_mismatch = false;
            bool vendor_mismatch = false;

            const auto first_security = group.front()->security_display;
            const auto first_vendor = group.front()->vendor;

            for (const auto* ap : group)
            {
                if (ap->security_display != first_security)
                {
                    security_mismatch = true;
                }

                if (!first_vendor.empty() && !ap->vendor.empty() && ap->vendor != first_vendor)
                {
                    vendor_mismatch = true;
                }
            }

            if (security_mismatch || vendor_mismatch)
            {
                std::string evidence = "SSID \"" + ssid + "\" was seen from " + std::to_string(group.size()) + " BSSID(s)";
                if (security_mismatch)
                {
                    evidence += " with differing security labels";
                }

                if (vendor_mismatch)
                {
                    evidence += security_mismatch ? " and differing vendors." : " with differing vendors.";
                }
                else
                {
                    evidence += ".";
                }

                AppendIssue(
                    report.issues,
                    "POSSIBLE_SSID_CLONE",
                    "Low",
                    "Possible SSID clone or inconsistent configuration",
                    "The same SSID was observed with conflicting characteristics. This may be normal in large deployments, but it deserves verification.",
                    evidence,
                    "Verify the expected security profile and vendor inventory for this SSID before treating it as suspicious.");
            }
        }

        for (const auto& warning : warnings)
        {
            AppendIssue(
                report.issues,
                "PLATFORM_WARNING",
                "Info",
                "Platform warning",
                "The platform scanner reported a warning that may affect data completeness.",
                warning,
                "Re-run the scan after clearing the warning condition.");
        }

        if (report.issues.empty())
        {
            AppendIssue(
                report.issues,
                "NO_MAJOR_ISSUES",
                "Info",
                "No major issues detected",
                "No obvious security or stability issues were detected from the current scan data.",
                "Analysis completed without high-confidence findings.",
                "Repeat scans under real conditions to catch intermittent problems.");
        }

        std::stable_sort(
            report.issues.begin(),
            report.issues.end(),
            [](const AnalysisIssue& left, const AnalysisIssue& right)
            {
                const auto left_weight = SeverityWeight(left.severity);
                const auto right_weight = SeverityWeight(right.severity);

                if (left_weight != right_weight)
                {
                    return left_weight > right_weight;
                }

                return left.code < right.code;
            });

        for (std::size_t i = 0; i < report.issues.size(); ++i)
        {
            report.issues[i].rank = static_cast<int32_t>(i + 1);
        }

        return report;
    }

    std::string SerializeReportAsJson(const AnalysisReport& report)
    {
        std::ostringstream json;

        json << "{";
        json << "\"schema\":\"" << JsonEscape(report.schema) << "\",";
        json << "\"sourcePlatform\":\"" << JsonEscape(report.source_platform) << "\",";
        json << "\"isPartial\":" << (report.is_partial ? "true" : "false") << ",";
        json << "\"capabilityMessage\":\"" << JsonEscape(report.capability_message) << "\",";

        json << "\"warnings\":[";
        for (std::size_t i = 0; i < report.warnings.size(); ++i)
        {
            if (i > 0) json << ",";
            json << "\"" << JsonEscape(report.warnings[i]) << "\"";
        }
        json << "],";

        json << "\"accessPoints\":[";
        for (std::size_t i = 0; i < report.access_points.size(); ++i)
        {
            const auto& ap = report.access_points[i];
            if (i > 0) json << ",";

            json << "{"
                 << "\"ssid\":\"" << JsonEscape(ap.ssid) << "\","
                 << "\"bssid\":\"" << JsonEscape(ap.bssid) << "\","
                 << "\"vendor\":\"" << JsonEscape(ap.vendor) << "\","
                 << "\"band\":\"" << JsonEscape(ap.band) << "\","
                 << "\"security\":\"" << JsonEscape(ap.security_display) << "\","
                 << "\"channel\":" << ap.channel << ","
                 << "\"frequencyMhz\":" << ap.frequency_mhz << ","
                 << "\"signalDbm\":" << ap.signal_dbm << ","
                 << "\"partialObservation\":" << (ap.partial_observation ? "true" : "false") << ","
                 << "\"confidenceScore\":" << std::fixed << std::setprecision(2) << ap.confidence_score
                 << "}";
        }
        json << "],";

        json << "\"issues\":[";
        for (std::size_t i = 0; i < report.issues.size(); ++i)
        {
            const auto& issue = report.issues[i];
            if (i > 0) json << ",";

            json << "{"
                 << "\"rank\":" << issue.rank << ","
                 << "\"code\":\"" << JsonEscape(issue.code) << "\","
                 << "\"severity\":\"" << JsonEscape(issue.severity) << "\","
                 << "\"title\":\"" << JsonEscape(issue.title) << "\","
                 << "\"description\":\"" << JsonEscape(issue.description) << "\","
                 << "\"evidence\":\"" << JsonEscape(issue.evidence) << "\","
                 << "\"fixSteps\":\"" << JsonEscape(issue.fix_steps) << "\""
                 << "}";
        }
        json << "]";

        json << "}";
        return json.str();
    }
}