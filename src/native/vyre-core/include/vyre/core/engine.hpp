#pragma once

#include <string>

namespace vyre::core {

struct BuildInfo final {
    std::string product_name;
    std::string version;
    std::string compiler;
    std::string platform;
    bool interop_enabled;
};

class Engine final {
public:
    static BuildInfo GetBuildInfo();
    static std::string GetBuildInfoString();

private:
    static std::string DetectCompiler();
    static std::string DetectPlatform();
};

} // namespace vyre::core
