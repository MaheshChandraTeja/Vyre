#include "vyre/core/capture.hpp"

#include <atomic>
#include <chrono>
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <map>
#include <mutex>
#include <sstream>
#include <string>
#include <thread>
#include <unordered_map>
#include <vector>

#if defined(VYRE_HAS_PCAP)
#include <pcap.h>
#endif

namespace vyre::core::capture
{
    namespace
    {
        struct CaptureSession
        {
            std::atomic<bool> stop_requested{false};
            std::atomic<bool> running{false};
            std::atomic<bool> completed{false};
            std::string output_path;
            std::string bpf_filter;
            std::string error_message;
            std::uint64_t packets_seen = 0;
            std::uint64_t packets_written = 0;
            std::uint64_t bytes_written = 0;
            std::vector<DetectionEvent> detections;
            std::thread worker;
        };

        std::mutex g_capture_mutex;
        std::unordered_map<std::int64_t, std::unique_ptr<CaptureSession>> g_capture_sessions;
        std::atomic<std::int64_t> g_next_capture_handle{1};

        std::string EscapeJson(const std::string_view value)
        {
            std::string escaped;
            for (const auto ch : value)
            {
                switch (ch)
                {
                case '\\': escaped += "\\\\"; break;
                case '"': escaped += "\\\""; break;
                case '\n': escaped += "\\n"; break;
                case '\r': escaped += "\\r"; break;
                case '\t': escaped += "\\t"; break;
                default: escaped.push_back(ch); break;
                }
            }
            return escaped;
        }

        void WriteU32(std::ofstream& out, std::uint32_t value)
        {
            out.write(reinterpret_cast<const char*>(&value), sizeof(value));
        }

        void WriteU16(std::ofstream& out, std::uint16_t value)
        {
            out.write(reinterpret_cast<const char*>(&value), sizeof(value));
        }

        void WriteBlockPadding(std::ofstream& out, std::size_t length)
        {
            const std::size_t padding = (4 - (length % 4)) % 4;
            static const char zeroes[4] = {0, 0, 0, 0};
            out.write(zeroes, static_cast<std::streamsize>(padding));
        }

        void WriteSectionHeaderBlock(std::ofstream& out)
        {
            constexpr std::uint32_t type = 0x0A0D0D0A;
            constexpr std::uint32_t total_length = 28;
            constexpr std::uint32_t bom = 0x1A2B3C4D;
            constexpr std::uint16_t major = 1;
            constexpr std::uint16_t minor = 0;
            constexpr std::uint64_t section_length = 0xFFFFFFFFFFFFFFFFULL;

            WriteU32(out, type);
            WriteU32(out, total_length);
            WriteU32(out, bom);
            WriteU16(out, major);
            WriteU16(out, minor);
            out.write(reinterpret_cast<const char*>(&section_length), sizeof(section_length));
            WriteU32(out, total_length);
        }

        void WriteInterfaceDescriptionBlock(std::ofstream& out, std::uint16_t linktype, std::uint32_t snaplen)
        {
            constexpr std::uint32_t type = 0x00000001;
            constexpr std::uint32_t total_length = 20;

            WriteU32(out, type);
            WriteU32(out, total_length);
            WriteU16(out, linktype);
            WriteU16(out, 0);
            WriteU32(out, snaplen);
            WriteU32(out, total_length);
        }

        void WriteEnhancedPacketBlock(
            std::ofstream& out,
            std::uint32_t interface_id,
            std::uint32_t timestamp_high,
            std::uint32_t timestamp_low,
            const std::uint8_t* data,
            std::uint32_t captured_length,
            std::uint32_t original_length)
        {
            constexpr std::uint32_t type = 0x00000006;
            const std::uint32_t padded_length = (captured_length + 3U) & ~3U;
            const std::uint32_t total_length = 32U + padded_length;

            WriteU32(out, type);
            WriteU32(out, total_length);
            WriteU32(out, interface_id);
            WriteU32(out, timestamp_high);
            WriteU32(out, timestamp_low);
            WriteU32(out, captured_length);
            WriteU32(out, original_length);
            out.write(reinterpret_cast<const char*>(data), static_cast<std::streamsize>(captured_length));
            if (padded_length > captured_length)
            {
                static const char zeroes[4] = {0, 0, 0, 0};
                out.write(zeroes, static_cast<std::streamsize>(padded_length - captured_length));
            }
            WriteU32(out, total_length);
        }

        void IncrementDetection(CaptureSession& session, const std::string& code, const std::string& title, const std::string& description)
        {
            auto it = std::find_if(
                session.detections.begin(),
                session.detections.end(),
                [&](const DetectionEvent& item) { return item.code == code; });

            if (it == session.detections.end())
            {
                session.detections.push_back(DetectionEvent{
                    .code = code,
                    .title = title,
                    .description = description,
                    .count = 1
                });
            }
            else
            {
                ++it->count;
            }
        }

        void ParseManagementFrame(CaptureSession& session, int linktype, const std::uint8_t* packet, std::uint32_t length)
        {
            const std::uint8_t* frame = packet;
            std::uint32_t frame_length = length;

            if (linktype == 127) // radiotap
            {
                if (length < 4)
                {
                    return;
                }

                const auto radiotap_length = static_cast<std::uint16_t>(packet[2] | (packet[3] << 8));
                if (radiotap_length >= length)
                {
                    return;
                }

                frame = packet + radiotap_length;
                frame_length = length - radiotap_length;
            }

            if (frame_length < 24)
            {
                return;
            }

            const std::uint16_t frame_control = frame[0] | (frame[1] << 8);
            const auto type = static_cast<std::uint8_t>((frame_control >> 2) & 0x3);
            const auto subtype = static_cast<std::uint8_t>((frame_control >> 4) & 0xF);

            if (type != 0) // management only
            {
                return;
            }

            switch (subtype)
            {
            case 8:
                IncrementDetection(session, "BEACON_FRAME", "Beacon frames seen", "Beacon management frames were observed.");
                break;
            case 4:
                IncrementDetection(session, "PROBE_REQUEST", "Probe requests seen", "Probe request management frames were observed.");
                break;
            case 5:
                IncrementDetection(session, "PROBE_RESPONSE", "Probe responses seen", "Probe response management frames were observed.");
                break;
            case 12:
                IncrementDetection(session, "DEAUTH_DETECTED", "Deauthentication frames detected", "Deauthentication frames were detected in the capture.");
                break;
            default:
                break;
            }

            if (subtype == 8 || subtype == 5)
            {
                if (frame_length < 36)
                {
                    return;
                }

                const std::uint8_t* ies = frame + 36;
                std::uint32_t remaining = frame_length - 36;
                bool saw_rsn = false;
                bool saw_legacy_wpa = false;

                while (remaining >= 2)
                {
                    const auto id = ies[0];
                    const auto len = ies[1];
                    if (remaining < static_cast<std::uint32_t>(2 + len))
                    {
                        break;
                    }

                    if (id == 48)
                    {
                        saw_rsn = true;
                    }
                    else if (id == 221 && len >= 4)
                    {
                        if (ies[2] == 0x00 && ies[3] == 0x50 && ies[4] == 0xF2 && ies[5] == 0x01)
                        {
                            saw_legacy_wpa = true;
                        }
                    }

                    ies += 2 + len;
                    remaining -= 2 + len;
                }

                if (saw_rsn)
                {
                    IncrementDetection(session, "RSN_IE", "RSN information elements detected", "RSN security information elements were present in management frames.");
                }

                if (saw_legacy_wpa)
                {
                    IncrementDetection(session, "WPA_IE", "Legacy WPA information elements detected", "Legacy WPA vendor information elements were present in management frames.");
                }
            }
        }

        std::string SerializeDevicesJson(const std::vector<CaptureDevice>& devices)
        {
            std::ostringstream json;
            json << "{\"devices\":[";
            for (std::size_t i = 0; i < devices.size(); ++i)
            {
                if (i > 0) json << ",";
                json << "{"
                     << "\"name\":\"" << EscapeJson(devices[i].name) << "\","
                     << "\"description\":\"" << EscapeJson(devices[i].description) << "\""
                     << "}";
            }
            json << "]}";
            return json.str();
        }

        std::string SerializeStatusJson(const CaptureSession& session)
        {
            std::ostringstream json;
            json << "{"
                 << "\"running\":" << (session.running ? "true" : "false") << ","
                 << "\"completed\":" << (session.completed ? "true" : "false") << ","
                 << "\"outputPath\":\"" << EscapeJson(session.output_path) << "\","
                 << "\"bpfFilter\":\"" << EscapeJson(session.bpf_filter) << "\","
                 << "\"errorMessage\":\"" << EscapeJson(session.error_message) << "\","
                 << "\"packetsSeen\":" << session.packets_seen << ","
                 << "\"packetsWritten\":" << session.packets_written << ","
                 << "\"bytesWritten\":" << session.bytes_written << ","
                 << "\"detections\":[";
            for (std::size_t i = 0; i < session.detections.size(); ++i)
            {
                if (i > 0) json << ",";
                const auto& d = session.detections[i];
                json << "{"
                     << "\"code\":\"" << EscapeJson(d.code) << "\","
                     << "\"title\":\"" << EscapeJson(d.title) << "\","
                     << "\"description\":\"" << EscapeJson(d.description) << "\","
                     << "\"count\":" << d.count
                     << "}";
            }
            json << "]}";
            return json.str();
        }

#if defined(VYRE_HAS_PCAP)
        void CaptureWorker(CaptureSession& session, const std::string device_name, const std::int32_t duration_seconds)
        {
            char errbuf[PCAP_ERRBUF_SIZE] = {};
            pcap_t* handle = pcap_create(device_name.c_str(), errbuf);
            if (handle == nullptr)
            {
                session.error_message = errbuf;
                session.completed = true;
                return;
            }

            pcap_set_snaplen(handle, 65535);
            pcap_set_promisc(handle, 1);
            pcap_set_timeout(handle, 1000);

            const auto activate_status = pcap_activate(handle);
            if (activate_status < 0)
            {
                session.error_message = pcap_geterr(handle);
                pcap_close(handle);
                session.completed = true;
                return;
            }

            if (!session.bpf_filter.empty())
            {
                bpf_program program{};
                if (pcap_compile(handle, &program, session.bpf_filter.c_str(), 1, PCAP_NETMASK_UNKNOWN) == 0)
                {
                    if (pcap_setfilter(handle, &program) != 0)
                    {
                        session.error_message = pcap_geterr(handle);
                    }
                    pcap_freecode(&program);
                }
                else
                {
                    session.error_message = pcap_geterr(handle);
                }
            }

            std::filesystem::create_directories(std::filesystem::path(session.output_path).parent_path());
            std::ofstream out(session.output_path, std::ios::binary | std::ios::trunc);
            if (!out.is_open())
            {
                session.error_message = "Failed to open output file for pcapng writing.";
                pcap_close(handle);
                session.completed = true;
                return;
            }

            const auto linktype = static_cast<std::uint16_t>(pcap_datalink(handle));
            WriteSectionHeaderBlock(out);
            WriteInterfaceDescriptionBlock(out, linktype, 65535);

            session.running = true;
            const auto started = std::chrono::steady_clock::now();

            while (!session.stop_requested)
            {
                const auto elapsed = std::chrono::steady_clock::now() - started;
                if (duration_seconds > 0 && elapsed >= std::chrono::seconds(duration_seconds))
                {
                    break;
                }

                pcap_pkthdr* header = nullptr;
                const u_char* data = nullptr;
                const auto rc = pcap_next_ex(handle, &header, &data);

                if (rc == 1 && header != nullptr && data != nullptr)
                {
                    ++session.packets_seen;

                    const std::uint64_t ts_micro =
                        static_cast<std::uint64_t>(header->ts.tv_sec) * 1000000ULL +
                        static_cast<std::uint64_t>(header->ts.tv_usec);

                    WriteEnhancedPacketBlock(
                        out,
                        0,
                        static_cast<std::uint32_t>(ts_micro >> 32U),
                        static_cast<std::uint32_t>(ts_micro & 0xFFFFFFFFULL),
                        reinterpret_cast<const std::uint8_t*>(data),
                        header->caplen,
                        header->len);

                    ++session.packets_written;
                    session.bytes_written += header->caplen;

                    ParseManagementFrame(session, pcap_datalink(handle), reinterpret_cast<const std::uint8_t*>(data), header->caplen);
                }
                else if (rc == 0)
                {
                    continue;
                }
                else if (rc == -1)
                {
                    session.error_message = pcap_geterr(handle);
                    break;
                }
                else if (rc == -2)
                {
                    break;
                }
            }

            session.running = false;
            session.completed = true;
            pcap_close(handle);
        }
#endif
    }

    std::string ListDevicesJson()
    {
#if !defined(VYRE_HAS_PCAP)
        return R"({"devices":[],"warning":"Packet capture support was not compiled in. Define VYRE_HAS_PCAP and link libpcap/Npcap."})";
#else
        char errbuf[PCAP_ERRBUF_SIZE] = {};
        pcap_if_t* alldevs = nullptr;

        if (pcap_findalldevs(&alldevs, errbuf) != 0)
        {
            std::ostringstream json;
            json << "{\"devices\":[],\"error\":\"" << EscapeJson(errbuf) << "\"}";
            return json.str();
        }

        std::vector<CaptureDevice> devices;
        for (auto* dev = alldevs; dev != nullptr; dev = dev->next)
        {
            devices.push_back(CaptureDevice{
                .name = dev->name != nullptr ? dev->name : "",
                .description = dev->description != nullptr ? dev->description : ""
            });
        }

        pcap_freealldevs(alldevs);
        return SerializeDevicesJson(devices);
#endif
    }

    std::int64_t StartCapture(const std::string& device_name, const std::string& output_path, const std::string& bpf_filter, const std::int32_t duration_seconds)
    {
        const auto handle = g_next_capture_handle.fetch_add(1);
        auto session = std::make_unique<CaptureSession>();
        session->output_path = output_path;
        session->bpf_filter = bpf_filter;

#if defined(VYRE_HAS_PCAP)
        session->worker = std::thread([ptr = session.get(), device_name, duration_seconds]
        {
            CaptureWorker(*ptr, device_name, duration_seconds);
        });
#else
        static_cast<void>(device_name);
        static_cast<void>(duration_seconds);
        session->error_message = "Packet capture support was not compiled in. Define VYRE_HAS_PCAP and link libpcap/Npcap.";
        session->completed = true;
#endif

        {
            const std::scoped_lock lock(g_capture_mutex);
            g_capture_sessions.insert_or_assign(handle, std::move(session));
        }

        return handle;
    }

    std::string GetCaptureStatusJson(const std::int64_t handle)
    {
        const std::scoped_lock lock(g_capture_mutex);
        const auto it = g_capture_sessions.find(handle);
        if (it == g_capture_sessions.end())
        {
            return R"({"running":false,"completed":true,"errorMessage":"Capture session not found.","detections":[]})";
        }

        return SerializeStatusJson(*it->second);
    }

    std::string StopCaptureJson(const std::int64_t handle)
    {
        std::unique_ptr<CaptureSession> session;

        {
            const std::scoped_lock lock(g_capture_mutex);
            const auto it = g_capture_sessions.find(handle);
            if (it == g_capture_sessions.end())
            {
                return R"({"running":false,"completed":true,"errorMessage":"Capture session not found.","detections":[]})";
            }

            session = std::move(it->second);
            g_capture_sessions.erase(it);
        }

        session->stop_requested = true;
        if (session->worker.joinable())
        {
            session->worker.join();
        }

        return SerializeStatusJson(*session);
    }
}
