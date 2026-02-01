#include "wifi_core/wifi_core.h"
#include "wifi_core/version.h"

namespace vyre::wifi {

const char* version() noexcept {
  return WIFI_CORE_VERSION;
}

const char* hello() noexcept {
  return "Hello, cross-platform misery";
}

} // namespace vyre::wifi
