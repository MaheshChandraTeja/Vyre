#pragma once

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

// Returns the required UTF-8 byte count including the null terminator.
// If the provided buffer is too small, the output is truncated and still null-terminated.
VYRE_API int vyre_get_build_info(char* buffer, int buffer_length);
VYRE_API int vyre_get_version(char* buffer, int buffer_length);

#ifdef __cplusplus
}
#endif
