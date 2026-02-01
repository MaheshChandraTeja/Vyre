#pragma once
#include <cstdint>
#include <string>
#include <vector>

#include "wifi/access_point.hpp"

namespace vyre::wifi {

struct ScanResult {
  std::string captured_at_utc;

  std::vector<AccessPoint> access_points;
};

}
