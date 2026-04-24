#include "vyre/core/scanner.hpp"

#include <memory>

namespace vyre::core
{
#if defined(_WIN32)
    std::unique_ptr<IWifiScanner> CreateWindowsWlanApiScanner();
#endif

    namespace
    {
        class UnsupportedScanner final : public IWifiScanner
        {
        public:
            ScanResult scan() override
            {
                return ScanResult{
                    .status = ScanStatusCode::NotSupported,
                    .error_message = "Wi-Fi scanning is not supported on this platform in the current milestone.",
                    .access_points = {}
                };
            }
        };
    }

    std::unique_ptr<IWifiScanner> CreateDefaultScanner()
    {
#if defined(_WIN32)
        return CreateWindowsWlanApiScanner();
#else
        return std::make_unique<UnsupportedScanner>();
#endif
    }
}