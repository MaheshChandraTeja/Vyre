#pragma once
#include <vector>
#include <string>
#include <optional>

#include "access_point.hpp"
#include "scan_error.hpp"

namespace wifi {

struct ScanResult {
    std::vector<AccessPoint> access_points;
};

class IScanner {
public:
    virtual ~IScanner() = default;
    virtual ScanResult scan() = 0;
};

}
