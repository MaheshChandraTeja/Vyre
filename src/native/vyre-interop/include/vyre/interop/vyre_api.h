#pragma once

#include <stdint.h>

#if defined(_WIN32)
  #if defined(VYRE_INTEROP_BUILDING)
    #define VYRE_API __declspec(dllexport)
  #else
    #define VYRE_API __declspec(dllimport)
  #endif
#elif defined(__GNUC__) && __GNUC__ >= 4
  #define VYRE_API __attribute__((visibility("default")))
#else
  #define VYRE_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef enum vyre_status_code_t {
    VYRE_STATUS_OK = 0,
    VYRE_STATUS_INVALID_ARGUMENT = 1,
    VYRE_STATUS_NOT_FOUND = 2,
    VYRE_STATUS_ENGINE_ERROR = 3,
    VYRE_STATUS_NOT_SUPPORTED = 4,
    VYRE_STATUS_WIFI_DISABLED = 5,
    VYRE_STATUS_NO_ADAPTER = 6,
    VYRE_STATUS_PERMISSION_DENIED = 7,
    VYRE_STATUS_SCAN_FAILED = 8,
    VYRE_STATUS_INTERNAL_ERROR = 9
} vyre_status_code_t;

typedef struct vyre_access_point_t {
    char* bssid;
    char* ssid;
    int32_t channel;
    int32_t rssi_dbm;
    int32_t frequency_mhz;
    char* security;
    int32_t hidden;
} vyre_access_point_t;

typedef struct vyre_scan_results_t {
    vyre_access_point_t* items;
    int32_t count;
} vyre_scan_results_t;

/* Metadata / diagnostics */
VYRE_API int32_t vyre_get_build_info(char** value);
VYRE_API int32_t vyre_get_version(char** value);

/* Analysis */
VYRE_API int32_t vyre_analyze_json(const char* scan_results_json_utf8, char** report_json_utf8);
VYRE_API int32_t vyre_submit_scan_results_json(const char* scan_results_json_utf8, char** report_json_utf8);

/* Scan lifecycle */
VYRE_API int32_t vyre_scan_start(int64_t* scan_handle);
VYRE_API int32_t vyre_scan_stop(int64_t scan_handle);
VYRE_API int32_t vyre_scan_get_results(int64_t scan_handle, vyre_scan_results_t* results);
VYRE_API int32_t vyre_scan_free_results(vyre_scan_results_t* results);

/* Error helpers */
VYRE_API int32_t vyre_get_last_error(char** value);
VYRE_API int32_t vyre_get_error_string(int32_t status_code, char** value);

/* Memory helpers */
VYRE_API void vyre_free_string(char* value);

VYRE_API_EXPORT int32_t vyre_list_capture_devices_json(char** out_json);
VYRE_API_EXPORT int32_t vyre_capture_start(
    const char* device_name_utf8,
    const char* output_path_utf8,
    const char* bpf_filter_utf8,
    int32_t duration_seconds,
    int64_t* out_capture_handle);
VYRE_API_EXPORT int32_t vyre_capture_get_status_json(int64_t capture_handle, char** out_json);
VYRE_API_EXPORT int32_t vyre_capture_stop(int64_t capture_handle, char** out_json);

#ifdef __cplusplus
}
#endif