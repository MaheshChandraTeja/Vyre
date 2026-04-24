#include "vyre/interop/vyre_api.h"
#include "vyre/core/engine.hpp"
#include "vyre/core/capture.hpp"
#include <cstdlib>
#include <cstring>
#include <exception>
#include <string>
#include <vector>

namespace
{
    thread_local std::string g_last_error;

    char* DuplicateString(const std::string& value)
    {
        auto* buffer = static_cast<char*>(std::malloc(value.size() + 1));
        if (buffer == nullptr)
        {
            return nullptr;
        }

        std::memcpy(buffer, value.c_str(), value.size() + 1);
        return buffer;
    }

    void SetLastError(const std::string& message)
    {
        g_last_error = message;
    }

    int32_t StatusFromMessage(const std::string& message)
    {
        if (message.find("not found") != std::string::npos)
        {
            return VYRE_STATUS_NOT_FOUND;
        }

        return VYRE_STATUS_ENGINE_ERROR;
    }

    int32_t TryDuplicateOutString(const std::string& value, char** out_value)
    {
        if (out_value == nullptr)
        {
            SetLastError("Output pointer was null.");
            return VYRE_STATUS_INVALID_ARGUMENT;
        }

        *out_value = DuplicateString(value);
        if (*out_value == nullptr)
        {
            SetLastError("Failed to allocate output string.");
            return VYRE_STATUS_INTERNAL_ERROR;
        }

        g_last_error.clear();
        return VYRE_STATUS_OK;
    }
}

extern "C" int32_t vyre_list_capture_devices_json(char** out_json)
{
    if (out_json == nullptr)
    {
        SetLastError("Capture devices output pointer was null.");
        return VYRE_STATUS_INVALID_ARGUMENT;
    }

    *out_json = DuplicateString(vyre::core::capture::ListDevicesJson());
    if (*out_json == nullptr)
    {
        SetLastError("Failed to allocate capture devices JSON.");
        return VYRE_STATUS_INTERNAL_ERROR;
    }

    return VYRE_STATUS_OK;
}

extern "C" int32_t vyre_capture_start(
    const char* device_name_utf8,
    const char* output_path_utf8,
    const char* bpf_filter_utf8,
    int32_t duration_seconds,
    int64_t* out_capture_handle)
{
    if (device_name_utf8 == nullptr || output_path_utf8 == nullptr || out_capture_handle == nullptr)
    {
        SetLastError("Capture start received invalid arguments.");
        return VYRE_STATUS_INVALID_ARGUMENT;
    }

    try
    {
        *out_capture_handle = vyre::core::capture::StartCapture(
            device_name_utf8,
            output_path_utf8,
            bpf_filter_utf8 != nullptr ? bpf_filter_utf8 : "",
            duration_seconds);
        return VYRE_STATUS_OK;
    }
    catch (const std::exception& ex)
    {
        SetLastError(std::string("Failed to start capture: ") + ex.what());
        return VYRE_STATUS_INTERNAL_ERROR;
    }
}

extern "C" int32_t vyre_capture_get_status_json(int64_t capture_handle, char** out_json)
{
    if (out_json == nullptr)
    {
        SetLastError("Capture status output pointer was null.");
        return VYRE_STATUS_INVALID_ARGUMENT;
    }

    *out_json = DuplicateString(vyre::core::capture::GetCaptureStatusJson(capture_handle));
    if (*out_json == nullptr)
    {
        SetLastError("Failed to allocate capture status JSON.");
        return VYRE_STATUS_INTERNAL_ERROR;
    }

    return VYRE_STATUS_OK;
}

extern "C" int32_t vyre_capture_stop(int64_t capture_handle, char** out_json)
{
    if (out_json == nullptr)
    {
        SetLastError("Capture stop output pointer was null.");
        return VYRE_STATUS_INVALID_ARGUMENT;
    }

    *out_json = DuplicateString(vyre::core::capture::StopCaptureJson(capture_handle));
    if (*out_json == nullptr)
    {
        SetLastError("Failed to allocate capture final status JSON.");
        return VYRE_STATUS_INTERNAL_ERROR;
    }

    return VYRE_STATUS_OK;
}

extern "C"
{
    int32_t vyre_get_build_info(char** value)
    {
        try
        {
            return TryDuplicateOutString(vyre::core::Engine::GetBuildInfoString(), value);
        }
        catch (const std::exception& ex)
        {
            SetLastError(ex.what());
            return VYRE_STATUS_ENGINE_ERROR;
        }
    }

    int32_t vyre_get_version(char** value)
    {
        try
        {
            return TryDuplicateOutString(vyre::core::Engine::GetVersionString(), value);
        }
        catch (const std::exception& ex)
        {
            SetLastError(ex.what());
            return VYRE_STATUS_ENGINE_ERROR;
        }
    }

    int32_t vyre_analyze_json(const char* scan_results_json_utf8, char** report_json_utf8)
    {
        if (scan_results_json_utf8 == nullptr)
        {
            SetLastError("Input JSON pointer was null.");
            return VYRE_STATUS_INVALID_ARGUMENT;
        }

        try
        {
            return TryDuplicateOutString(
                vyre::core::Engine::AnalyzeResultsJson(scan_results_json_utf8),
                report_json_utf8);
        }
        catch (const std::exception& ex)
        {
            SetLastError(ex.what());
            return VYRE_STATUS_ENGINE_ERROR;
        }
    }

    int32_t vyre_submit_scan_results_json(const char* scan_results_json_utf8, char** report_json_utf8)
    {
        return vyre_analyze_json(scan_results_json_utf8, report_json_utf8);
    }

    int32_t vyre_scan_start(int64_t* scan_handle)
    {
        if (scan_handle == nullptr)
        {
            SetLastError("Scan handle output pointer was null.");
            return VYRE_STATUS_INVALID_ARGUMENT;
        }

        try
        {
            *scan_handle = vyre::core::Engine::StartScan();
            g_last_error.clear();
            return VYRE_STATUS_OK;
        }
        catch (const std::exception& ex)
        {
            *scan_handle = 0;
            SetLastError(ex.what());
            return VYRE_STATUS_ENGINE_ERROR;
        }
    }

    int32_t vyre_scan_stop(int64_t scan_handle)
    {
        try
        {
            const bool stopped = vyre::core::Engine::StopScan(scan_handle);
            if (!stopped)
            {
                SetLastError("Scan session not found.");
                return VYRE_STATUS_NOT_FOUND;
            }

            g_last_error.clear();
            return VYRE_STATUS_OK;
        }
        catch (const std::exception& ex)
        {
            SetLastError(ex.what());
            return VYRE_STATUS_ENGINE_ERROR;
        }
    }

    int32_t vyre_scan_get_results(int64_t scan_handle, vyre_scan_results_t* results)
    {
        if (results == nullptr)
        {
            SetLastError("Scan results output pointer was null.");
            return VYRE_STATUS_INVALID_ARGUMENT;
        }

        results->items = nullptr;
        results->count = 0;

        try
        {
            const std::vector<vyre::core::AccessPoint> access_points = vyre::core::Engine::GetScanResults(scan_handle);
            if (access_points.empty())
            {
                g_last_error.clear();
                return VYRE_STATUS_OK;
            }

            results->items = static_cast<vyre_access_point_t*>(std::calloc(access_points.size(), sizeof(vyre_access_point_t)));
            if (results->items == nullptr)
            {
                SetLastError("Failed to allocate scan result items.");
                return VYRE_STATUS_INTERNAL_ERROR;
            }

            results->count = static_cast<int32_t>(access_points.size());

            for (std::size_t index = 0; index < access_points.size(); ++index)
            {
                const auto& source = access_points[index];
                auto& target = results->items[index];

                target.bssid = DuplicateString(source.bssid);
                target.ssid = DuplicateString(source.ssid);
                target.channel = source.channel;
                target.rssi_dbm = source.rssi_dbm;
                target.frequency_mhz = source.frequency_mhz;
                target.security = DuplicateString(source.security);
                target.hidden = source.hidden ? 1 : 0;

                if (target.bssid == nullptr || target.ssid == nullptr || target.security == nullptr)
                {
                    vyre_scan_free_results(results);
                    SetLastError("Failed to allocate scan result strings.");
                    return VYRE_STATUS_INTERNAL_ERROR;
                }
            }

            g_last_error.clear();
            return VYRE_STATUS_OK;
        }
        catch (const std::exception& ex)
        {
            SetLastError(ex.what());
            return StatusFromMessage(g_last_error);
        }
    }

    int32_t vyre_scan_free_results(vyre_scan_results_t* results)
    {
        if (results == nullptr)
        {
            return VYRE_STATUS_INVALID_ARGUMENT;
        }

        if (results->items != nullptr)
        {
            for (int32_t index = 0; index < results->count; ++index)
            {
                std::free(results->items[index].bssid);
                std::free(results->items[index].ssid);
                std::free(results->items[index].security);
            }

            std::free(results->items);
        }

        results->items = nullptr;
        results->count = 0;
        return VYRE_STATUS_OK;
    }

    int32_t vyre_get_last_error(char** value)
    {
        return TryDuplicateOutString(g_last_error, value);
    }

    int32_t vyre_get_error_string(int32_t status_code, char** value)
    {
        const std::string message = [status_code]
        {
            switch (status_code)
            {
            case VYRE_STATUS_OK: return std::string("No error.");
            case VYRE_STATUS_INVALID_ARGUMENT: return std::string("Invalid argument.");
            case VYRE_STATUS_NOT_FOUND: return std::string("Requested item was not found.");
            case VYRE_STATUS_ENGINE_ERROR: return std::string("Engine error.");
            case VYRE_STATUS_NOT_SUPPORTED: return std::string("Operation is not supported.");
            case VYRE_STATUS_WIFI_DISABLED: return std::string("Wi-Fi is disabled.");
            case VYRE_STATUS_NO_ADAPTER: return std::string("No Wi-Fi adapter found.");
            case VYRE_STATUS_PERMISSION_DENIED: return std::string("Permission denied.");
            case VYRE_STATUS_SCAN_FAILED: return std::string("Scan failed.");
            case VYRE_STATUS_INTERNAL_ERROR: return std::string("Internal error.");
            default: return std::string("Unknown error.");
            }
        }();

        return TryDuplicateOutString(message, value);
    }

    void vyre_free_string(char* value)
    {
        std::free(value);
    }
}
