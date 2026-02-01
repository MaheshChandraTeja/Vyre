#pragma once

#ifdef _WIN32
  #ifdef WIFI_INTEROP_BUILD
    #define WIFI_API __declspec(dllexport)
  #else
    #define WIFI_API __declspec(dllimport)
  #endif
#else
  #define WIFI_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

WIFI_API const char* vyre_wifi_version();
WIFI_API const char* vyre_wifi_hello();

#ifdef __cplusplus
}
#endif
