#include "vyre/core/engine.hpp"
#include "vyre/interop/vyre_api.h"

#include <cassert>
#include <cstring>
#include <iostream>
#include <vector>

int main() {
    const auto info = vyre::core::Engine::GetBuildInfo();
    assert(info.product_name == "Vyre");
    assert(!info.version.empty());
    assert(!info.compiler.empty());
    assert(!info.platform.empty());

    std::vector<char> buffer(256, '\0');
    const int required = vyre_get_build_info(buffer.data(), static_cast<int>(buffer.size()));
    assert(required > 0);
    assert(std::strlen(buffer.data()) > 0);

    std::vector<char> version_buffer(32, '\0');
    const int version_required = vyre_get_version(version_buffer.data(), static_cast<int>(version_buffer.size()));
    assert(version_required > 0);
    assert(std::strlen(version_buffer.data()) > 0);

    std::cout << "Native smoke tests passed. Build info: " << buffer.data() << std::endl;
    return 0;
}
