#pragma once

#include <memory>
#include <string>
#include <vector>

namespace vyre::core
{
    enum class ScanStatusCode
    {
        Ok = 0,
        NotSupported = 1,
        WifiDisabled = 2,
        NoAdapter = 3,
        PermissionDenied = 4,
        ScanFailed = 5,
        InvalidArgument = 6,
        InternalError = 7
    };

    struct AccessPoint
    {
        std::string ssid;
        std::string bssid;
        std::string band;
        int channel = 0;
        int signal_dbm = 0;
        std::string security;
    };

    struct ScanResult
    {
        ScanStatusCode status = ScanStatusCode::Ok;
        std::string error_message;
        std::vector<AccessPoint> access_points;
    };

    class IWifiScanner
    {
    public:
        virtual ~IWifiScanner() = default;
        virtual ScanResult scan() = 0;
    };

    std::unique_ptr<IWifiScanner> CreateDefaultScanner();
}