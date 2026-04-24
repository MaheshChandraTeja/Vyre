#include "vyre/core/analysis.hpp"

#include <algorithm>
#include <cctype>
#include <cmath>
#include <sstream>

namespace vyre::core::analysis
{
    namespace
    {
        std::string ToUpperAscii(std::string_view value)
        {
            std::string result;
            result.reserve(value.size());
            for (const auto ch : value)
            {
                result.push_back(static_cast<char>(std::toupper(static_cast<unsigned char>(ch))));
            }
            return result;
        }

        bool ContainsInsensitive(std::string_view haystack, std::string_view needle)
        {
            const auto upper_haystack = ToUpperAscii(haystack);
            const auto upper_needle = ToUpperAscii(needle);
            return upper_haystack.find(upper_needle) != std::string::npos;
        }

        double Clamp01(double value)
        {
            if (value < 0.0)
            {
                return 0.0;
            }

            if (value > 1.0)
            {
                return 1.0;
            }

            return value;
        }

        std::string SecurityCategoryToStableLabel(SecurityCategory category)
        {
            switch (category)
            {
            case SecurityCategory::Open: return "Open";
            case SecurityCategory::Wep: return "WEP";
            case SecurityCategory::Wpa: return "WPA";
            case SecurityCategory::Wpa2: return "WPA2";
            case SecurityCategory::Wpa3: return "WPA3";
            case SecurityCategory::Enterprise: return "Enterprise";
            case SecurityCategory::EnhancedOpen: return "Enhanced Open";
            case SecurityCategory::Unknown:
            default:
                return "Unknown";
            }
        }
    }

    std::string NormalizeOui(std::string_view bssid)
    {
        std::string hex;
        hex.reserve(12);

        for (const auto ch : bssid)
        {
            if (std::isxdigit(static_cast<unsigned char>(ch)) != 0)
            {
                hex.push_back(static_cast<char>(std::toupper(static_cast<unsigned char>(ch))));
            }
        }

        if (hex.size() < 6)
        {
            return {};
        }

        return hex.substr(0, 6);
    }

    std::string NormalizeBandFromFrequency(const int32_t frequency_mhz)
    {
        if (frequency_mhz >= 2400 && frequency_mhz < 2500)
        {
            return "2.4 GHz";
        }

        if (frequency_mhz >= 4900 && frequency_mhz < 5925)
        {
            return "5 GHz";
        }

        if (frequency_mhz >= 5925 && frequency_mhz < 7125)
        {
            return "6 GHz";
        }

        return "Unknown";
    }

    int32_t NormalizeChannel(const int32_t channel, const int32_t frequency_mhz)
    {
        if (channel > 0)
        {
            return channel;
        }

        if (frequency_mhz == 2484)
        {
            return 14;
        }

        if (frequency_mhz >= 2412 && frequency_mhz <= 2472)
        {
            return (frequency_mhz - 2407) / 5;
        }

        if (frequency_mhz >= 5000 && frequency_mhz <= 5895)
        {
            return (frequency_mhz - 5000) / 5;
        }

        if (frequency_mhz >= 5955 && frequency_mhz <= 7115)
        {
            return (frequency_mhz - 5950) / 5;
        }

        return 0;
    }

    SecurityCategory NormalizeSecurityCategory(std::string_view raw_security)
    {
        const auto value = ToUpperAscii(raw_security);

        if (value.empty())
        {
            return SecurityCategory::Unknown;
        }

        if (value.find("OWE") != std::string::npos || value.find("ENHANCED OPEN") != std::string::npos)
        {
            return SecurityCategory::EnhancedOpen;
        }

        if (value.find("WPA3") != std::string::npos || value.find("SAE") != std::string::npos)
        {
            return SecurityCategory::Wpa3;
        }

        if (value.find("RSN") != std::string::npos || value.find("WPA2") != std::string::npos)
        {
            if (value.find("EAP") != std::string::npos || value.find("ENTERPRISE") != std::string::npos)
            {
                return SecurityCategory::Enterprise;
            }

            return SecurityCategory::Wpa2;
        }

        if (value.find("WPA") != std::string::npos)
        {
            if (value.find("EAP") != std::string::npos || value.find("ENTERPRISE") != std::string::npos)
            {
                return SecurityCategory::Enterprise;
            }

            return SecurityCategory::Wpa;
        }

        if (value.find("WEP") != std::string::npos)
        {
            return SecurityCategory::Wep;
        }

        if (value == "OPEN" || value == "[ESS]" || value.find("NONE") != std::string::npos)
        {
            return SecurityCategory::Open;
        }

        return SecurityCategory::Unknown;
    }

    std::string NormalizeSecurityDisplay(const std::string_view raw_security, const SecurityCategory category)
    {
        const auto value = ToUpperAscii(raw_security);

        switch (category)
        {
        case SecurityCategory::Open:
            return "Open";
        case SecurityCategory::Wep:
            return "WEP";
        case SecurityCategory::EnhancedOpen:
            return "Enhanced Open (OWE)";
        case SecurityCategory::Wpa3:
            if (value.find("ENTERPRISE") != std::string::npos || value.find("EAP") != std::string::npos)
            {
                return "WPA3 Enterprise";
            }
            return "WPA3";
        case SecurityCategory::Wpa2:
            if (value.find("PERSONAL") != std::string::npos || value.find("PSK") != std::string::npos)
            {
                return "WPA2 Personal";
            }
            return "WPA2";
        case SecurityCategory::Wpa:
            if (value.find("PERSONAL") != std::string::npos || value.find("PSK") != std::string::npos)
            {
                return "WPA Personal";
            }
            return "WPA";
        case SecurityCategory::Enterprise:
            if (value.find("WPA3") != std::string::npos)
            {
                return "WPA3 Enterprise";
            }

            if (value.find("WPA2") != std::string::npos || value.find("RSN") != std::string::npos)
            {
                return "WPA2 Enterprise";
            }

            return "Enterprise";
        case SecurityCategory::Unknown:
        default:
            if (!raw_security.empty())
            {
                return std::string(raw_security);
            }

            return SecurityCategoryToStableLabel(category);
        }
    }

    double ComputeConfidenceScore(
        const std::string_view source_platform,
        const bool is_partial,
        const std::string_view bssid,
        const int32_t channel,
        const int32_t frequency_mhz,
        const std::string_view raw_security)
    {
        double score = 0.75;

        if (ContainsInsensitive(source_platform, "WINDOWS"))
        {
            score = 0.97;
        }
        else if (ContainsInsensitive(source_platform, "ANDROID"))
        {
            score = 0.93;
        }
        else if (ContainsInsensitive(source_platform, "IOS"))
        {
            score = 0.52;
        }

        if (is_partial)
        {
            score -= 0.22;
        }

        if (bssid.empty())
        {
            score -= 0.18;
        }

        if (channel <= 0 && frequency_mhz <= 0)
        {
            score -= 0.12;
        }

        if (raw_security.empty())
        {
            score -= 0.08;
        }

        return Clamp01(score);
    }

    std::vector<NormalizedAccessPoint> NormalizeAccessPoints(
        const std::vector<RawAccessPoint>& access_points,
        const std::string_view source_platform,
        const bool is_partial,
        const OuiVendorLookup& oui_lookup)
    {
        std::vector<NormalizedAccessPoint> normalized;
        normalized.reserve(access_points.size());

        for (const auto& item : access_points)
        {
            const auto category = NormalizeSecurityCategory(item.security);
            const auto normalized_channel = NormalizeChannel(item.channel, item.frequency_mhz);
            const auto vendor = oui_lookup.LookupVendor(item.bssid);

            normalized.push_back(NormalizedAccessPoint{
                .ssid = item.hidden && item.ssid.empty() ? "<Hidden>" : item.ssid,
                .bssid = item.bssid,
                .vendor = vendor,
                .band = NormalizeBandFromFrequency(item.frequency_mhz),
                .security_display = NormalizeSecurityDisplay(item.security, category),
                .security_category = category,
                .channel = normalized_channel,
                .frequency_mhz = item.frequency_mhz,
                .signal_dbm = item.signal_dbm,
                .hidden = item.hidden,
                .partial_observation = is_partial,
                .confidence_score = ComputeConfidenceScore(
                    source_platform,
                    is_partial,
                    item.bssid,
                    normalized_channel,
                    item.frequency_mhz,
                    item.security)
            });
        }

        std::sort(
            normalized.begin(),
            normalized.end(),
            [](const NormalizedAccessPoint& left, const NormalizedAccessPoint& right)
            {
                if (left.signal_dbm != right.signal_dbm)
                {
                    return left.signal_dbm > right.signal_dbm;
                }

                if (left.ssid != right.ssid)
                {
                    return left.ssid < right.ssid;
                }

                return left.bssid < right.bssid;
            });

        return normalized;
    }
}