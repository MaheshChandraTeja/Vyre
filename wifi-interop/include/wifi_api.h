#pragma once

#include <stddef.h>
#include <stdint.h>

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

typedef enum VyreWifiErrorCode {
  VYRE_WIFI_OK = 0,

  VYRE_WIFI_ERR_INVALID_ARGUMENT = 1,
  VYRE_WIFI_ERR_PARSE_FAILED     = 2,
  VYRE_WIFI_ERR_INTERNAL         = 3,
  VYRE_WIFI_ERR_NOT_IMPLEMENTED  = 4
} VyreWifiErrorCode;

WIFI_API const char* vyre_wifi_error_string(VyreWifiErrorCode code);

WIFI_API const char* vyre_wifi_last_error_message(void);

WIFI_API void vyre_wifi_free(void* p);

WIFI_API VyreWifiErrorCode vyre_wifi_get_version(char** out_utf8);

WIFI_API VyreWifiErrorCode vyre_wifi_analyze_json(
  const char* scan_json_utf8,
  char** out_report_json_utf8
);


WIFI_API VyreWifiErrorCode vyre_wifi_scan_start(void);
WIFI_API VyreWifiErrorCode vyre_wifi_scan_stop(void);

WIFI_API VyreWifiErrorCode vyre_wifi_scan_get_results(char** out_scan_json_utf8);

#ifdef __cplusplus
}
#endif
