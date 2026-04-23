#pragma once

#include <string_view>

namespace vyre::core {

struct Version final {
    static constexpr int Major = VYRE_CORE_VERSION_MAJOR;
    static constexpr int Minor = VYRE_CORE_VERSION_MINOR;
    static constexpr int Patch = VYRE_CORE_VERSION_PATCH;
    static constexpr std::string_view SemVer = "0.1.0";
};

} // namespace vyre::core
