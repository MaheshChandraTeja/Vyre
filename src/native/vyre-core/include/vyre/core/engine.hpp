#pragma once

#include <cstdint>
#include <string>
#include <string_view>
#include <vector>

namespace vyre::core {

struct BuildInfo final {
    std::string product_name;
    std::string version;
    std::string compiler;
    std::string platform;
    bool interop_enabled;
};

struct AccessPoint final {
    std::string bssid;
    std::string ssid;
    std::int32_t channel;
    std::int32_t rssi_dbm;
    std::int32_t frequency_mhz;
    std::string security;
    bool hidden;
};

class Engine final {
public:
    static BuildInfo GetBuildInfo();
    static std::string GetBuildInfoString();
    static std::string GetVersionString();

    static std::int64_t StartScan();
    static bool StopScan(std::int64_t scan_handle);
    static std::vector<AccessPoint> GetScanResults(std::int64_t scan_handle);
    static std::string AnalyzeResultsJson(std::string_view scan_results_json);

private:
    static std::string DetectCompiler();
    static std::string DetectPlatform();
};

} // namespace vyre::core
