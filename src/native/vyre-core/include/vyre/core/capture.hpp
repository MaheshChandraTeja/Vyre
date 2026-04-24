#pragma once

#include <cstdint>
#include <string>
#include <vector>

namespace vyre::core::capture
{
    struct CaptureDevice
    {
        std::string name;
        std::string description;
    };

    struct DetectionEvent
    {
        std::string code;
        std::string title;
        std::string description;
        std::uint64_t count = 0;
    };

    struct CaptureStatus
    {
        bool running = false;
        bool completed = false;
        std::string output_path;
        std::string error_message;
        std::string bpf_filter;
        std::uint64_t packets_seen = 0;
        std::uint64_t packets_written = 0;
        std::uint64_t bytes_written = 0;
        std::vector<DetectionEvent> detections;
    };

    std::string ListDevicesJson();
    std::int64_t StartCapture(const std::string& device_name, const std::string& output_path, const std::string& bpf_filter, std::int32_t duration_seconds);
    std::string GetCaptureStatusJson(std::int64_t handle);
    std::string StopCaptureJson(std::int64_t handle);
}