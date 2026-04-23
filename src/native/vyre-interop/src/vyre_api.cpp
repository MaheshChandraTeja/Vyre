#include "vyre/interop/vyre_api.h"

#include "vyre/core/engine.hpp"
#include "vyre/core/version.hpp"

#include <algorithm>
#include <cstring>
#include <string>

namespace {

int CopyToCallerBuffer(const std::string& value, char* buffer, const int buffer_length) {
    const int required_length = static_cast<int>(value.size()) + 1;

    if (buffer == nullptr || buffer_length <= 0) {
        return required_length;
    }

    const int copy_length = std::max(0, std::min(static_cast<int>(value.size()), buffer_length - 1));
    std::memcpy(buffer, value.data(), static_cast<std::size_t>(copy_length));
    buffer[copy_length] = '\0';
    return required_length;
}

} // namespace

extern "C" {

int vyre_get_build_info(char* buffer, const int buffer_length) {
    return CopyToCallerBuffer(vyre::core::Engine::GetBuildInfoString(), buffer, buffer_length);
}

int vyre_get_version(char* buffer, const int buffer_length) {
    return CopyToCallerBuffer(std::string(vyre::core::Version::SemVer), buffer, buffer_length);
}

} // extern "C"
