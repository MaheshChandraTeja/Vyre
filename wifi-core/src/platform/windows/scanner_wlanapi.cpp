#ifdef _WIN32

#include <windows.h>
#include <wlanapi.h>
#include <objbase.h>
#include <sstream>
#include <vector>

#include "wifi/scanner.hpp"
#include "wifi/scan_error.hpp"

#pragma comment(lib, "wlanapi.lib")
#pragma comment(lib, "ole32.lib")

namespace wifi {

static std::string ssid_to_string(const DOT11_SSID& ssid) {
    return std::string(reinterpret_cast<const char*>(ssid.ucSSID),
                       ssid.uSSIDLength);
}

static std::string security_to_string(const WLAN_SECURITY_ATTRIBUTES& sec) {
    if (!sec.bSecurityEnabled)
        return "Open";

    switch (sec.dot11AuthAlgorithm) {
    case DOT11_AUTH_ALGO_WPA:
    case DOT11_AUTH_ALGO_WPA_PSK:
        return "WPA";
    case DOT11_AUTH_ALGO_RSNA:
    case DOT11_AUTH_ALGO_RSNA_PSK:
        return "WPA2/WPA3";
    default:
        return "Unknown";
    }
}

class WindowsWlanScanner final : public IScanner {
public:
    ScanResult scan() override {
        HANDLE client = nullptr;
        DWORD negotiated = 0;

        if (WlanOpenHandle(2, nullptr, &negotiated, &client) != ERROR_SUCCESS)
            throw ScanError("Failed to open WLAN handle");

        PWLAN_INTERFACE_INFO_LIST if_list = nullptr;
        if (WlanEnumInterfaces(client, nullptr, &if_list) != ERROR_SUCCESS) {
            WlanCloseHandle(client, nullptr);
            throw ScanError("No WiFi adapters found");
        }

        if (if_list->dwNumberOfItems == 0) {
            WlanFreeMemory(if_list);
            WlanCloseHandle(client, nullptr);
            throw ScanError("WiFi disabled or no interfaces present");
        }

        ScanResult result;

        for (DWORD i = 0; i < if_list->dwNumberOfItems; ++i) {
            const auto& iface = if_list->InterfaceInfo[i];

            PWLAN_BSS_LIST bss_list = nullptr;
            if (WlanGetNetworkBssList(
                    client,
                    &iface.InterfaceGuid,
                    nullptr,
                    dot11_BSS_type_any,
                    FALSE,
                    nullptr,
                    &bss_list) != ERROR_SUCCESS)
                continue;

            for (DWORD j = 0; j < bss_list->dwNumberOfItems; ++j) {
                const auto& bss = bss_list->wlanBssEntries[j];

                AccessPoint ap;
                ap.ssid = ssid_to_string(bss.dot11Ssid);
                ap.bssid = format_bssid(bss.dot11Bssid);
                ap.rssi_dbm = bss.lRssi;
                ap.channel = frequency_to_channel(bss.ulChCenterFrequency);
                ap.band = (bss.ulChCenterFrequency < 3000000) ? "2.4ghz" : "5ghz";
                ap.security = security_to_string(bss.wlanSecurityAttributes);

                result.access_points.push_back(std::move(ap));
            }

            WlanFreeMemory(bss_list);
        }

        WlanFreeMemory(if_list);
        WlanCloseHandle(client, nullptr);

        return result;
    }

private:
    static std::string format_bssid(const UCHAR bssid[6]) {
        char buf[18];
        snprintf(buf, sizeof(buf), "%02X:%02X:%02X:%02X:%02X:%02X",
                 bssid[0], bssid[1], bssid[2],
                 bssid[3], bssid[4], bssid[5]);
        return buf;
    }

    static int frequency_to_channel(ULONG freq_khz) {
        if (freq_khz >= 2412000 && freq_khz <= 2484000)
            return (freq_khz - 2407000) / 5000;
        if (freq_khz >= 5170000 && freq_khz <= 5835000)
            return (freq_khz - 5000000) / 5000;
        return -1;
    }
};

}
#endif
