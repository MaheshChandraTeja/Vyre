#define WIFI_INTEROP_BUILD
#include "wifi_interop/wifi_capi.h"
#include "wifi_core/wifi_core.h"

const char* vyre_wifi_version() {
  return vyre::wifi::version();
}

const char* vyre_wifi_hello() {
  return vyre::wifi::hello();
}
