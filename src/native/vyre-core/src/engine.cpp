#include "vyre/core/engine.hpp"
#include "vyre/core/version.hpp"

#include <sstream>

#if defined(__APPLE__)
#include <TargetConditionals.h>
#endif

namespace vyre::core {

std::string Engine::DetectCompiler() {
#if defined(_MSC_VER)
    return "MSVC " + std::to_string(_MSC_VER);
#elif defined(__clang__)
    return "Clang " + std::to_string(__clang_major__) + "." + std::to_string(__clang_minor__);
#elif defined(__GNUC__)
    return "GCC " + std::to_string(__GNUC__) + "." + std::to_string(__GNUC_MINOR__);
#else
    return "UnknownCompiler";
#endif
}

std::string Engine::DetectPlatform() {
#if defined(__ANDROID__)
    return "Android";
#elif defined(__APPLE__) && defined(TARGET_OS_IPHONE) && TARGET_OS_IPHONE
    return "iOS";
#elif defined(__APPLE__)
    return "Apple";
#elif defined(_WIN32)
    return "Windows";
#elif defined(__linux__)
    return "Linux";
#else
    return "UnknownPlatform";
#endif
}

BuildInfo Engine::GetBuildInfo() {
    return BuildInfo{
        .product_name = "Vyre",
        .version = std::string(Version::SemVer),
        .compiler = DetectCompiler(),
        .platform = DetectPlatform(),
        .interop_enabled = true,
    };
}

std::string Engine::GetBuildInfoString() {
    const BuildInfo info = GetBuildInfo();

    std::ostringstream stream;
    stream << info.product_name
           << "/" << info.version
           << " | compiler=" << info.compiler
           << " | platform=" << info.platform
           << " | interop=" << (info.interop_enabled ? "enabled" : "disabled");

    return stream.str();
}

} // namespace vyre::core
