#pragma once
#include <cstdint>
#include <optional>
#include <string>
#include <string_view>

namespace vyre::wifi {

enum class Band : std::uint8_t { Unknown = 0, GHz2_4, GHz5, GHz6 };

struct AccessPoint {
  std::string bssid;
  std::string ssid;

  Band band = Band::Unknown;

  std::optional<int> channel;
  std::optional<int> rssi_dbm; 
  std::optional<std::string> security;
};

inline std::string_view band_to_string(Band b) {
  switch (b) {
    case Band::GHz2_4: return "2.4ghz";
    case Band::GHz5:   return "5ghz";
    case Band::GHz6:   return "6ghz";
    default:           return "unknown";
  }
}

}
