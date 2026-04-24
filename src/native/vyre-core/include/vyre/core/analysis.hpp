#pragma once

#include <cstdint>
#include <memory>
#include <string>
#include <string_view>
#include <unordered_map>
#include <vector>

namespace vyre::core::analysis
{
    enum class SecurityCategory
    {
        Unknown = 0,
        Open,
        Wep,
        Wpa,
        Wpa2,
        Wpa3,
        Enterprise,
        EnhancedOpen
    };

    struct RawAccessPoint
    {
        std::string ssid;
        std::string bssid;
        std::string security;
        int32_t channel = 0;
        int32_t frequency_mhz = 0;
        int32_t signal_dbm = 0;
        bool hidden = false;
    };

    struct NormalizedAccessPoint
    {
        std::string ssid;
        std::string bssid;
        std::string vendor;
        std::string band;
        std::string security_display;
        SecurityCategory security_category = SecurityCategory::Unknown;
        int32_t channel = 0;
        int32_t frequency_mhz = 0;
        int32_t signal_dbm = 0;
        bool hidden = false;
        bool partial_observation = false;
        double confidence_score = 0.0;
    };

    struct AnalysisIssue
    {
        std::string code;
        std::string severity;
        std::string title;
        std::string description;
        std::string evidence;
        std::string fix_steps;
        int32_t rank = 0;
    };

    struct AnalysisReport
    {
        std::string schema = "vyre.report.v1";
        std::string source_platform;
        bool is_partial = false;
        std::string capability_message;
        std::vector<std::string> warnings;
        std::vector<NormalizedAccessPoint> access_points;
        std::vector<AnalysisIssue> issues;
    };

    class OuiVendorLookup
    {
    public:
        OuiVendorLookup() = default;

        bool LoadFromCsvFile(const std::string& path);
        bool LoadFromCsvText(std::string_view csv_text);
        std::string LookupVendor(std::string_view bssid) const;

    private:
        std::unordered_map<std::string, std::string> entries_;
    };

    std::string NormalizeOui(std::string_view bssid);
    std::string NormalizeBandFromFrequency(int32_t frequency_mhz);
    int32_t NormalizeChannel(int32_t channel, int32_t frequency_mhz);
    SecurityCategory NormalizeSecurityCategory(std::string_view raw_security);
    std::string NormalizeSecurityDisplay(std::string_view raw_security, SecurityCategory category);
    double ComputeConfidenceScore(
        std::string_view source_platform,
        bool is_partial,
        std::string_view bssid,
        int32_t channel,
        int32_t frequency_mhz,
        std::string_view raw_security);

    std::vector<NormalizedAccessPoint> NormalizeAccessPoints(
        const std::vector<RawAccessPoint>& access_points,
        std::string_view source_platform,
        bool is_partial,
        const OuiVendorLookup& oui_lookup);

    AnalysisReport AnalyzeNormalizedAccessPoints(
        std::string_view source_platform,
        bool is_partial,
        std::string_view capability_message,
        const std::vector<std::string>& warnings,
        const std::vector<NormalizedAccessPoint>& access_points);

    std::string SerializeReportAsJson(const AnalysisReport& report);
}