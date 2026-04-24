#include "vyre/core/scanner.hpp"

#if defined(_WIN32)

#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <windows.h>
#include <wlanapi.h>
#include <windot11.h>

#include <algorithm>
#include <cstdint>
#include <iomanip>
#include <memory>
#include <optional>
#include <set>
#include <sstream>
#include <string>
#include <unordered_map>
#include <vector>

namespace vyre::core
{
    namespace
    {
        class WlanHandle final
        {
        public:
            explicit WlanHandle(HANDLE value) noexcept : value_(value) {}
            ~WlanHandle()
            {
                if (value_ != nullptr)
                {
                    WlanCloseHandle(value_, nullptr);
                }
            }

            WlanHandle(const WlanHandle&) = delete;
            WlanHandle& operator=(const WlanHandle&) = delete;

            HANDLE get() const noexcept { return value_; }

        private:
            HANDLE value_ = nullptr;
        };

        template <typename T>
        class WlanMemory final
        {
        public:
            explicit WlanMemory(T* value = nullptr) noexcept : value_(value) {}
            ~WlanMemory()
            {
                if (value_ != nullptr)
                {
                    WlanFreeMemory(value_);
                }
            }

            WlanMemory(const WlanMemory&) = delete;
            WlanMemory& operator=(const WlanMemory&) = delete;

            T* get() const noexcept { return value_; }
            T** put() noexcept { return &value_; }

        private:
            T* value_ = nullptr;
        };

        std::string ToSsidString(const DOT11_SSID& ssid)
        {
            if (ssid.uSSIDLength == 0)
            {
                return "<Hidden>";
            }

            return std::string(
                reinterpret_cast<const char*>(ssid.ucSSID),
                reinterpret_cast<const char*>(ssid.ucSSID) + ssid.uSSIDLength);
        }

        std::string ToBssidString(const UCHAR bssid[6])
        {
            std::ostringstream stream;
            stream << std::hex << std::setfill('0') << std::uppercase
                   << std::setw(2) << static_cast<int>(bssid[0]) << ":"
                   << std::setw(2) << static_cast<int>(bssid[1]) << ":"
                   << std::setw(2) << static_cast<int>(bssid[2]) << ":"
                   << std::setw(2) << static_cast<int>(bssid[3]) << ":"
                   << std::setw(2) << static_cast<int>(bssid[4]) << ":"
                   << std::setw(2) << static_cast<int>(bssid[5]);
            return stream.str();
        }

        int FrequencyKhzToChannel(ULONG frequency_khz)
        {
            const auto mhz = static_cast<int>(frequency_khz / 1000UL);

            if (mhz == 2484)
            {
                return 14;
            }

            if (mhz >= 2412 && mhz <= 2472)
            {
                return (mhz - 2407) / 5;
            }

            if (mhz >= 5000 && mhz <= 5895)
            {
                return (mhz - 5000) / 5;
            }

            if (mhz >= 5955 && mhz <= 7115)
            {
                return (mhz - 5950) / 5;
            }

            return 0;
        }

        std::string FrequencyKhzToBand(ULONG frequency_khz)
        {
            const auto mhz = static_cast<int>(frequency_khz / 1000UL);

            if (mhz >= 2400 && mhz < 2500)
            {
                return "2.4 GHz";
            }

            if (mhz >= 5000 && mhz < 5925)
            {
                return "5 GHz";
            }

            if (mhz >= 5925 && mhz < 7125)
            {
                return "6 GHz";
            }

            return "Unknown";
        }

        std::string AuthAlgorithmToString(DOT11_AUTH_ALGORITHM value)
        {
            switch (value)
            {
            case DOT11_AUTH_ALGO_80211_OPEN: return "Open";
            case DOT11_AUTH_ALGO_80211_SHARED_KEY: return "WEP";
            case DOT11_AUTH_ALGO_WPA: return "WPA-Enterprise";
            case DOT11_AUTH_ALGO_WPA_PSK: return "WPA-Personal";
            case DOT11_AUTH_ALGO_RSNA: return "WPA2-Enterprise";
            case DOT11_AUTH_ALGO_RSNA_PSK: return "WPA2-Personal";
#ifdef DOT11_AUTH_ALGO_WPA3
            case DOT11_AUTH_ALGO_WPA3: return "WPA3";
#endif
#ifdef DOT11_AUTH_ALGO_WPA3_SAE
            case DOT11_AUTH_ALGO_WPA3_SAE: return "WPA3-Personal";
#endif
#ifdef DOT11_AUTH_ALGO_OWE
            case DOT11_AUTH_ALGO_OWE: return "OWE";
#endif
            default: return "Secured";
            }
        }

        std::string CipherAlgorithmToString(DOT11_CIPHER_ALGORITHM value)
        {
            switch (value)
            {
            case DOT11_CIPHER_ALGO_NONE: return "None";
            case DOT11_CIPHER_ALGO_WEP40: return "WEP40";
            case DOT11_CIPHER_ALGO_TKIP: return "TKIP";
            case DOT11_CIPHER_ALGO_CCMP: return "CCMP";
            case DOT11_CIPHER_ALGO_WEP104: return "WEP104";
#ifdef DOT11_CIPHER_ALGO_GCMP
            case DOT11_CIPHER_ALGO_GCMP: return "GCMP";
#endif
#ifdef DOT11_CIPHER_ALGO_GCMP_256
            case DOT11_CIPHER_ALGO_GCMP_256: return "GCMP-256";
#endif
            default: return "Unknown";
            }
        }

        std::string BuildSecurityString(const WLAN_AVAILABLE_NETWORK& network)
        {
            if (!network.bSecurityEnabled)
            {
                return "Open";
            }

            const auto auth = AuthAlgorithmToString(network.dot11DefaultAuthAlgorithm);
            const auto cipher = CipherAlgorithmToString(network.dot11DefaultCipherAlgorithm);

            if (auth == "WEP")
            {
                return "WEP";
            }

            if (cipher == "None" || cipher == "Unknown")
            {
                return auth;
            }

            return auth + " / " + cipher;
        }

        std::string FallbackSecurityFromCapability(USHORT capability_bits)
        {
            constexpr USHORT privacy_bit = 0x0010;
            return (capability_bits & privacy_bit) != 0 ? "Secured" : "Open";
        }

        std::string FormatErrorCode(DWORD code)
        {
            std::ostringstream stream;
            stream << "Windows WLAN API error code " << code;
            return stream.str();
        }

        class WindowsWlanApiScanner final : public IWifiScanner
        {
        public:
            ScanResult scan() override
            {
                DWORD negotiated_version = 0;
                HANDLE raw_handle = nullptr;
                const DWORD open_status = WlanOpenHandle(2, nullptr, &negotiated_version, &raw_handle);

                if (open_status != ERROR_SUCCESS || raw_handle == nullptr)
                {
                    return Failure(
                        ScanStatusCode::ScanFailed,
                        "Failed to open the Windows WLAN API handle. " + FormatErrorCode(open_status));
                }

                WlanHandle client_handle(raw_handle);

                WlanMemory<WLAN_INTERFACE_INFO_LIST> interface_list;
                const DWORD enum_status = WlanEnumInterfaces(client_handle.get(), nullptr, interface_list.put());

                if (enum_status != ERROR_SUCCESS)
                {
                    return Failure(
                        ScanStatusCode::ScanFailed,
                        "Failed to enumerate Wi-Fi adapters. " + FormatErrorCode(enum_status));
                }

                if (interface_list.get() == nullptr || interface_list.get()->dwNumberOfItems == 0)
                {
                    return Failure(ScanStatusCode::NoAdapter, "No wireless adapter was found.");
                }

                std::vector<AccessPoint> access_points;
                access_points.reserve(64);

                std::set<std::string> dedupe_keys;
                bool saw_wifi_disabled = false;
                bool scanned_any_interface = false;

                for (DWORD i = 0; i < interface_list.get()->dwNumberOfItems; ++i)
                {
                    const auto& iface = interface_list.get()->InterfaceInfo[i];

                    const DWORD trigger_scan_status = WlanScan(client_handle.get(), &iface.InterfaceGuid, nullptr, nullptr, nullptr);
                    if (trigger_scan_status == ERROR_SUCCESS)
                    {
                        scanned_any_interface = true;
                    }
                    else if (trigger_scan_status == ERROR_NDIS_DOT11_POWER_STATE_INVALID)
                    {
                        saw_wifi_disabled = true;
                    }
                }

                if (scanned_any_interface)
                {
                    ::Sleep(1200);
                }

                for (DWORD i = 0; i < interface_list.get()->dwNumberOfItems; ++i)
                {
                    const auto& iface = interface_list.get()->InterfaceInfo[i];

                    std::unordered_map<std::string, std::string> security_by_ssid;

                    WlanMemory<WLAN_AVAILABLE_NETWORK_LIST> available_networks;
                    const DWORD available_status = WlanGetAvailableNetworkList(
                        client_handle.get(),
                        &iface.InterfaceGuid,
                        0,
                        nullptr,
                        available_networks.put());

                    if (available_status == ERROR_NDIS_DOT11_POWER_STATE_INVALID)
                    {
                        saw_wifi_disabled = true;
                        continue;
                    }

                    if (available_status == ERROR_SUCCESS && available_networks.get() != nullptr)
                    {
                        for (DWORD n = 0; n < available_networks.get()->dwNumberOfItems; ++n)
                        {
                            const auto& network = available_networks.get()->Network[n];
                            security_by_ssid[ToSsidString(network.dot11Ssid)] = BuildSecurityString(network);
                        }
                    }

                    WlanMemory<WLAN_BSS_LIST> bss_list;
                    const DWORD bss_status = WlanGetNetworkBssList(
                        client_handle.get(),
                        &iface.InterfaceGuid,
                        nullptr,
                        dot11_BSS_type_any,
                        FALSE,
                        nullptr,
                        bss_list.put());

                    if (bss_status == ERROR_NDIS_DOT11_POWER_STATE_INVALID)
                    {
                        saw_wifi_disabled = true;
                        continue;
                    }

                    if (bss_status != ERROR_SUCCESS)
                    {
                        continue;
                    }

                    if (bss_list.get() == nullptr)
                    {
                        continue;
                    }

                    for (DWORD b = 0; b < bss_list.get()->dwNumberOfItems; ++b)
                    {
                        const auto& entry = bss_list.get()->wlanBssEntries[b];

                        const auto ssid = ToSsidString(entry.dot11Ssid);
                        const auto bssid = ToBssidString(entry.dot11Bssid);
                        const auto key = ssid + "|" + bssid;

                        if (!dedupe_keys.insert(key).second)
                        {
                            continue;
                        }

                        auto security_it = security_by_ssid.find(ssid);
                        const auto security = security_it != security_by_ssid.end()
                            ? security_it->second
                            : FallbackSecurityFromCapability(entry.usCapabilityInformation);

                        access_points.push_back(AccessPoint{
                            .ssid = ssid,
                            .bssid = bssid,
                            .band = FrequencyKhzToBand(entry.ulChCenterFrequency),
                            .channel = FrequencyKhzToChannel(entry.ulChCenterFrequency),
                            .signal_dbm = static_cast<int>(entry.lRssi),
                            .security = security
                        });
                    }
                }

                if (access_points.empty())
                {
                    if (saw_wifi_disabled)
                    {
                        return Failure(ScanStatusCode::WifiDisabled, "Wi-Fi appears to be disabled.");
                    }

                    return ScanResult{
                        .status = ScanStatusCode::Ok,
                        .error_message = {},
                        .access_points = {}
                    };
                }

                std::sort(
                    access_points.begin(),
                    access_points.end(),
                    [](const AccessPoint& left, const AccessPoint& right)
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

                return ScanResult{
                    .status = ScanStatusCode::Ok,
                    .error_message = {},
                    .access_points = std::move(access_points)
                };
            }

        private:
            static ScanResult Failure(ScanStatusCode status, std::string message)
            {
                return ScanResult{
                    .status = status,
                    .error_message = std::move(message),
                    .access_points = {}
                };
            }
        };
    }

    std::unique_ptr<IWifiScanner> CreateWindowsWlanApiScanner()
    {
        return std::make_unique<WindowsWlanApiScanner>();
    }
}

#endif