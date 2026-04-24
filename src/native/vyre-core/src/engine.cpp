#include "vyre/core/engine.hpp"
#include "vyre/core/version.hpp"
#include "vyre/core/analysis.hpp"

#include <algorithm>
#include <atomic>
#include <cmath>
#include <cstdint>
#include <fstream>
#include <mutex>
#include <regex>
#include <sstream>
#include <stdexcept>
#include <string>
#include <string_view>
#include <unordered_map>
#include <utility>
#include <vector>

#if defined(__APPLE__)
#include <TargetConditionals.h>
#endif

namespace vyre::core {
namespace {

using vyre::core::analysis::AnalysisReport;
using vyre::core::analysis::NormalizeAccessPoints;
using vyre::core::analysis::OuiVendorLookup;
using vyre::core::analysis::RawAccessPoint;

struct ScanSession final {
    bool active;
    std::vector<AccessPoint> results;
};

std::mutex g_scan_mutex;
std::unordered_map<std::int64_t, ScanSession> g_sessions;
std::atomic<std::int64_t> g_next_scan_handle{1};

std::string EscapeJson(const std::string_view value) {
    std::string escaped;
    escaped.reserve(value.size() + 8);

    for (const char ch : value) {
        switch (ch) {
        case '\\':
            escaped += "\\\\";
            break;
        case '"':
            escaped += "\\\"";
            break;
        case '\b':
            escaped += "\\b";
            break;
        case '\f':
            escaped += "\\f";
            break;
        case '\n':
            escaped += "\\n";
            break;
        case '\r':
            escaped += "\\r";
            break;
        case '\t':
            escaped += "\\t";
            break;
        default:
            escaped.push_back(ch);
            break;
        }
    }

    return escaped;
}

std::string Quote(const std::string_view value) {
    return "\"" + EscapeJson(value) + "\"";
}

std::vector<AccessPoint> BuildDeterministicSampleResults() {
    return {
        AccessPoint{
            .bssid = "34:12:98:AB:CD:EF",
            .ssid = "Kairais-5G",
            .channel = 36,
            .rssi_dbm = -42,
            .frequency_mhz = 5180,
            .security = "WPA3",
            .hidden = false,
        },
        AccessPoint{
            .bssid = "14:23:45:67:89:10",
            .ssid = "Office-IoT",
            .channel = 6,
            .rssi_dbm = -67,
            .frequency_mhz = 2437,
            .security = "WPA2",
            .hidden = false,
        },
        AccessPoint{
            .bssid = "AA:BB:CC:DD:EE:FF",
            .ssid = "",
            .channel = 149,
            .rssi_dbm = -73,
            .frequency_mhz = 5745,
            .security = "Open",
            .hidden = true,
        },
    };
}

std::vector<std::string> MatchObjects(const std::string_view json) {
    std::vector<std::string> matches;
    const std::regex object_regex(R"(\{[^{}]*\})");
    const std::string text(json);

    for (std::sregex_iterator it(text.begin(), text.end(), object_regex), end; it != end; ++it) {
        matches.push_back(it->str());
    }

    return matches;
}

std::string ExtractStringField(const std::string& object, const std::string& field_name) {
    const std::string pattern = "\"" + field_name + "\"\\s*:\\s*\"([^\"]*)\"";
    const std::regex field_regex(pattern);
    std::smatch match;
    if (std::regex_search(object, match, field_regex) && match.size() > 1) {
        return match[1].str();
    }

    return {};
}

std::int32_t ExtractIntField(const std::string& object, const std::string& field_name) {
    const std::string pattern = "\"" + field_name + "\"\\s*:\\s*(-?\\d+)";
    const std::regex field_regex(pattern);
    std::smatch match;
    if (std::regex_search(object, match, field_regex) && match.size() > 1) {
        return static_cast<std::int32_t>(std::stoi(match[1].str()));
    }

    return 0;
}

bool ExtractBoolField(const std::string& object, const std::string& field_name) {
    const std::string pattern = "\"" + field_name + "\"\\s*:\\s*(true|false)";
    const std::regex field_regex(pattern);
    std::smatch match;
    if (std::regex_search(object, match, field_regex) && match.size() > 1) {
        return match[1].str() == "true";
    }

    return false;
}

std::vector<AccessPoint> ParseAccessPointsFromJson(const std::string_view scan_results_json) {
    std::vector<AccessPoint> access_points;
    const auto objects = MatchObjects(scan_results_json);

    for (const auto& object : objects) {
        const auto bssid = ExtractStringField(object, "bssid");
        const auto ssid = ExtractStringField(object, "ssid");
        const auto security = ExtractStringField(object, "security");

        if (bssid.empty() && ssid.empty() && security.empty()) {
            continue;
        }

        access_points.push_back(AccessPoint{
            .bssid = bssid,
            .ssid = ssid,
            .channel = ExtractIntField(object, "channel"),
            .rssi_dbm = ExtractIntField(object, "rssiDbm"),
            .frequency_mhz = ExtractIntField(object, "frequencyMhz"),
            .security = security,
            .hidden = ExtractBoolField(object, "hidden"),
        });
    }

    return access_points;
}

std::string ExtractTopLevelStringField(const std::string_view json, const std::string& field_name) {
    const std::string pattern = "\"" + field_name + "\"\\s*:\\s*\"([^\"]*)\"";
    const std::regex field_regex(pattern);
    std::smatch match;
    const std::string text(json);
    if (std::regex_search(text, match, field_regex) && match.size() > 1) {
        return match[1].str();
    }

    return {};
}

bool ExtractTopLevelBoolField(const std::string_view json, const std::string& field_name) {
    const std::string pattern = "\"" + field_name + "\"\\s*:\\s*(true|false)";
    const std::regex field_regex(pattern);
    std::smatch match;
    const std::string text(json);
    if (std::regex_search(text, match, field_regex) && match.size() > 1) {
        return match[1].str() == "true";
    }

    return false;
}

std::vector<std::string> ExtractStringArrayField(const std::string_view json, const std::string& field_name) {
    std::vector<std::string> values;

    const std::string pattern = "\"" + field_name + "\"\\s*:\\s*\\[(.*?)\\]";
    const std::regex array_regex(pattern);
    std::smatch array_match;
    const std::string text(json);

    if (!std::regex_search(text, array_match, array_regex) || array_match.size() < 2) {
        return values;
    }

    const std::string array_content = array_match[1].str();
    const std::regex string_regex(R"json("([^"]*)")json");

    for (std::sregex_iterator it(array_content.begin(), array_content.end(), string_regex), end; it != end; ++it) {
        if (it->size() > 1) {
            values.push_back((*it)[1].str());
        }
    }

    return values;
}

std::vector<RawAccessPoint> ToRawAccessPoints(const std::vector<AccessPoint>& access_points) {
    std::vector<RawAccessPoint> raw;
    raw.reserve(access_points.size());

    for (const auto& access_point : access_points) {
        raw.push_back(RawAccessPoint{
            .ssid = access_point.ssid,
            .bssid = access_point.bssid,
            .security = access_point.security,
            .channel = access_point.channel,
            .frequency_mhz = access_point.frequency_mhz,
            .signal_dbm = access_point.rssi_dbm,
            .hidden = access_point.hidden,
        });
    }

    return raw;
}

OuiVendorLookup BuildOuiLookup() {
    OuiVendorLookup lookup;

#if defined(VYRE_CORE_OUI_DB_RELATIVE_PATH)
    static_cast<void>(lookup.LoadFromCsvFile(VYRE_CORE_OUI_DB_RELATIVE_PATH));
#endif

    if (!lookup.LookupVendor("00:1A:2B:00:00:00").empty()) {
        return lookup;
    }

    static constexpr std::string_view kFallbackOuiCsv =
        "# OUI,Vendor\n"
        "001A2B,Example Networks\n"
        "3C5A37,Google\n"
        "A4CF12,Apple\n"
        "F4F5D8,Samsung\n"
        "D850E6,Intel\n"
        "9C5CF9,TP-Link\n"
        "B4FBF9,Ubiquiti\n"
        "C83A35,Netgear\n"
        "00155D,Cisco\n"
        "E4956E,ASUSTek\n";

    static_cast<void>(lookup.LoadFromCsvText(kFallbackOuiCsv));
    return lookup;
}

std::string BuildLegacyFallbackReportJson(const std::vector<AccessPoint>& access_points) {
    const std::size_t total_networks = access_points.size();
    if (total_networks == 0U) {
        return R"({"schemaVersion":"1.0","summary":{"totalNetworks":0,"averageSignalDbm":0,"hiddenNetworks":0,"band24Count":0,"band5Count":0,"strongestNetwork":{"ssid":"","bssid":"","rssiDbm":0}},"issues":[{"code":"EMPTY_SCAN","severity":"warning","message":"No access points were supplied for analysis."}],"recommendations":["Collect a scan payload before running analysis."]})";
    }

    const auto strongest = std::max_element(
        access_points.begin(),
        access_points.end(),
        [](const AccessPoint& left, const AccessPoint& right) {
            return left.rssi_dbm < right.rssi_dbm;
        });

    double total_signal = 0.0;
    std::int32_t hidden_networks = 0;
    std::int32_t band24_count = 0;
    std::int32_t band5_count = 0;
    std::int32_t crowded_channels = 0;

    std::unordered_map<std::int32_t, std::int32_t> channel_counts;
    for (const auto& access_point : access_points) {
        total_signal += static_cast<double>(access_point.rssi_dbm);

        if (access_point.hidden) {
            ++hidden_networks;
        }

        if (access_point.frequency_mhz >= 2400 && access_point.frequency_mhz < 2500) {
            ++band24_count;
        } else if (access_point.frequency_mhz >= 5000) {
            ++band5_count;
        }

        const auto count = ++channel_counts[access_point.channel];
        if (count == 3) {
            ++crowded_channels;
        }
    }

    const auto average_signal = static_cast<std::int32_t>(std::lround(total_signal / static_cast<double>(total_networks)));

    std::vector<std::string> issues;
    std::vector<std::string> recommendations;

    if (band24_count > band5_count) {
        issues.push_back(R"({"code":"BAND_BALANCE","severity":"info","message":"The environment is more crowded on 2.4 GHz than on 5 GHz."})");
        recommendations.emplace_back("Prefer 5 GHz or 6 GHz where client capability allows.");
    }

    if (crowded_channels > 0) {
        issues.push_back(R"({"code":"CHANNEL_CONTENTION","severity":"warning","message":"Multiple access points are sharing the same channel, which may reduce throughput."})");
        recommendations.emplace_back("Run channel planning and avoid overlapping AP assignments.");
    }

    if (hidden_networks > 0) {
        issues.push_back(R"({"code":"HIDDEN_NETWORKS","severity":"info","message":"Hidden SSIDs were detected in the scan snapshot."})");
        recommendations.emplace_back("Review whether hidden SSIDs are intentional and still necessary.");
    }

    if (issues.empty()) {
        issues.push_back(R"({"code":"NO_MAJOR_ISSUES","severity":"info","message":"No obvious RF issues were detected in the supplied snapshot."})");
        recommendations.emplace_back("Capture repeated scans across time before changing network design.");
    }

    std::ostringstream stream;
    stream << "{"
           << "\"schemaVersion\":\"1.0\","
           << "\"summary\":{"
           << "\"totalNetworks\":" << total_networks << ","
           << "\"averageSignalDbm\":" << average_signal << ","
           << "\"hiddenNetworks\":" << hidden_networks << ","
           << "\"band24Count\":" << band24_count << ","
           << "\"band5Count\":" << band5_count << ","
           << "\"strongestNetwork\":{"
           << "\"ssid\":" << Quote(strongest->ssid) << ","
           << "\"bssid\":" << Quote(strongest->bssid) << ","
           << "\"rssiDbm\":" << strongest->rssi_dbm
           << "}"
           << "},"
           << "\"issues\":[";

    for (std::size_t index = 0; index < issues.size(); ++index) {
        if (index > 0U) {
            stream << ",";
        }

        stream << issues[index];
    }

    stream << "],\"recommendations\":[";

    for (std::size_t index = 0; index < recommendations.size(); ++index) {
        if (index > 0U) {
            stream << ",";
        }

        stream << Quote(recommendations[index]);
    }

    stream << "]}";
    return stream.str();
}

std::string BuildReportJson(const std::string_view scan_results_json) {
    const auto parsed_access_points = ParseAccessPointsFromJson(scan_results_json);

    const auto source_platform = ExtractTopLevelStringField(scan_results_json, "sourcePlatform");
    const auto capability_message = ExtractTopLevelStringField(scan_results_json, "capabilityMessage");
    const auto is_partial = ExtractTopLevelBoolField(scan_results_json, "isPartial");
    const auto warnings = ExtractStringArrayField(scan_results_json, "warnings");

    try {
        auto oui_lookup = BuildOuiLookup();
        const auto raw_access_points = ToRawAccessPoints(parsed_access_points);

        const auto normalized = NormalizeAccessPoints(
            raw_access_points,
            source_platform.empty() ? std::string_view{"Unknown"} : std::string_view{source_platform},
            is_partial,
            oui_lookup);

        const AnalysisReport report = analysis::AnalyzeNormalizedAccessPoints(
            source_platform.empty() ? std::string_view{"Unknown"} : std::string_view{source_platform},
            is_partial,
            capability_message,
            warnings,
            normalized);

        return analysis::SerializeReportAsJson(report);
    }
    catch (...) {
        return BuildLegacyFallbackReportJson(parsed_access_points);
    }
}

} // namespace

std::string Engine::DetectCompiler() {
#if defined(_MSC_VER)
    return "MSVC " + std::to_string(_MSC_VER);
#elif defined(__clang__)
    return "Clang " + std::to_string(__clang_major__) + "." + std::to_string(__clang_minor__);
#elif defined(__GNUC__)
    return "GCC " + std::to_string(__GNUC__) + "." + std::to_string(__GNUC_MINOR__);
#else
    return "UnknownCompiler";
#endif
}

std::string Engine::DetectPlatform() {
#if defined(__ANDROID__)
    return "Android";
#elif defined(__APPLE__) && defined(TARGET_OS_IPHONE) && TARGET_OS_IPHONE
    return "iOS";
#elif defined(__APPLE__)
    return "Apple";
#elif defined(_WIN32)
    return "Windows";
#elif defined(__linux__)
    return "Linux";
#else
    return "UnknownPlatform";
#endif
}

BuildInfo Engine::GetBuildInfo() {
    return BuildInfo{
        .product_name = "Vyre",
        .version = std::string(Version::SemVer),
        .compiler = DetectCompiler(),
        .platform = DetectPlatform(),
        .interop_enabled = true,
    };
}

std::string Engine::GetBuildInfoString() {
    const BuildInfo info = GetBuildInfo();

    std::ostringstream stream;
    stream << info.product_name
           << "/" << info.version
           << " | compiler=" << info.compiler
           << " | platform=" << info.platform
           << " | interop=" << (info.interop_enabled ? "enabled" : "disabled");

    return stream.str();
}

std::string Engine::GetVersionString() {
    return std::string(Version::SemVer);
}

std::int64_t Engine::StartScan() {
    const std::int64_t scan_handle = g_next_scan_handle.fetch_add(1);
    ScanSession session{
        .active = true,
        .results = BuildDeterministicSampleResults(),
    };

    const std::scoped_lock lock(g_scan_mutex);
    g_sessions.insert_or_assign(scan_handle, std::move(session));
    return scan_handle;
}

bool Engine::StopScan(const std::int64_t scan_handle) {
    const std::scoped_lock lock(g_scan_mutex);
    return g_sessions.erase(scan_handle) > 0U;
}

std::vector<AccessPoint> Engine::GetScanResults(const std::int64_t scan_handle) {
    const std::scoped_lock lock(g_scan_mutex);
    const auto it = g_sessions.find(scan_handle);
    if (it == g_sessions.end() || !it->second.active) {
        throw std::runtime_error("Scan session not found.");
    }

    return it->second.results;
}

std::string Engine::AnalyzeResultsJson(const std::string_view scan_results_json) {
    return BuildReportJson(scan_results_json);
}

} // namespace vyre::core
