#include "vyre/core/reporting.hpp"

#include <algorithm>
#include <filesystem>
#include <fstream>
#include <map>
#include <regex>
#include <sstream>
#include <unordered_map>

namespace vyre::core::reporting
{
    namespace
    {
        std::string EscapeJson(const std::string_view value)
        {
            std::string escaped;
            escaped.reserve(value.size() + 8);

            for (const char ch : value)
            {
                switch (ch)
                {
                case '\\': escaped += "\\\\"; break;
                case '"': escaped += "\\\""; break;
                case '\b': escaped += "\\b"; break;
                case '\f': escaped += "\\f"; break;
                case '\n': escaped += "\\n"; break;
                case '\r': escaped += "\\r"; break;
                case '\t': escaped += "\\t"; break;
                default: escaped.push_back(ch); break;
                }
            }

            return escaped;
        }

        std::string Quote(const std::string_view value)
        {
            return "\"" + EscapeJson(value) + "\"";
        }

        std::string ExtractStringField(const std::string& text, const std::string& field_name)
        {
            const std::regex rx("\"" + field_name + "\"\\s*:\\s*\"([^\"]*)\"");
            std::smatch match;
            if (std::regex_search(text, match, rx) && match.size() > 1)
            {
                return match[1].str();
            }

            return {};
        }

        std::int64_t ExtractInt64Field(const std::string& text, const std::string& field_name)
        {
            const std::regex rx("\"" + field_name + "\"\\s*:\\s*(-?\\d+)");
            std::smatch match;
            if (std::regex_search(text, match, rx) && match.size() > 1)
            {
                return static_cast<std::int64_t>(std::stoll(match[1].str()));
            }

            return 0;
        }

        std::string SerializeReportRecord(const ReportRecord& record)
        {
            std::ostringstream stream;
            stream << "{"
                   << "\"id\":" << Quote(record.id) << ","
                   << "\"capturedAtUnixUtc\":" << record.captured_at_unix_utc << ","
                   << "\"report\":" << analysis::SerializeReportAsJson(record.report)
                   << "}";
            return stream.str();
        }

        bool WriteTextFile(const std::string& output_path, const std::string& content)
        {
            std::filesystem::create_directories(std::filesystem::path(output_path).parent_path());
            std::ofstream out(output_path, std::ios::binary | std::ios::trunc);
            if (!out.is_open())
            {
                return false;
            }

            out.write(content.data(), static_cast<std::streamsize>(content.size()));
            return out.good();
        }

        std::string SecurityDisplay(const analysis::NormalizedAccessPoint& ap)
        {
            return ap.security_display.empty() ? "Unknown" : ap.security_display;
        }
    }

    bool SaveReportRecordAsJson(const ReportRecord& record, const std::string& output_path)
    {
        return WriteTextFile(output_path, SerializeReportRecord(record));
    }

    bool LoadReportRecordFromJson(const std::string& input_path, ReportRecord& out_record)
    {
        std::ifstream in(input_path, std::ios::binary);
        if (!in.is_open())
        {
            return false;
        }

        std::ostringstream buffer;
        buffer << in.rdbuf();
        const auto text = buffer.str();

        out_record.id = ExtractStringField(text, "id");
        out_record.captured_at_unix_utc = ExtractInt64Field(text, "capturedAtUnixUtc");

        const auto report_start = text.find("\"report\":");
        if (report_start == std::string::npos)
        {
            return false;
        }

        const auto brace_start = text.find('{', report_start);
        if (brace_start == std::string::npos)
        {
            return false;
        }

        int depth = 0;
        std::size_t brace_end = std::string::npos;
        for (std::size_t i = brace_start; i < text.size(); ++i)
        {
            if (text[i] == '{') ++depth;
            if (text[i] == '}')
            {
                --depth;
                if (depth == 0)
                {
                    brace_end = i;
                    break;
                }
            }
        }

        if (brace_end == std::string::npos)
        {
            return false;
        }

        const auto report_json = text.substr(brace_start, brace_end - brace_start + 1);

        out_record.report.schema = ExtractStringField(report_json, "schema");
        out_record.report.source_platform = ExtractStringField(report_json, "sourcePlatform");
        out_record.report.capability_message = ExtractStringField(report_json, "capabilityMessage");
        out_record.report.is_partial = report_json.find("\"isPartial\":true") != std::string::npos;

        out_record.report.warnings.clear();
        out_record.report.access_points.clear();
        out_record.report.issues.clear();

        const std::regex ap_regex(R"(\{"ssid":"([^"]*)","bssid":"([^"]*)","vendor":"([^"]*)","band":"([^"]*)","security":"([^"]*)","channel":(-?\d+),"frequencyMhz":(-?\d+),"signalDbm":(-?\d+),"partialObservation":(true|false),"confidenceScore":([0-9.]+)\})");
        for (std::sregex_iterator it(report_json.begin(), report_json.end(), ap_regex), end; it != end; ++it)
        {
            const auto& m = *it;
            out_record.report.access_points.push_back(analysis::NormalizedAccessPoint{
                .ssid = m[1].str(),
                .bssid = m[2].str(),
                .vendor = m[3].str(),
                .band = m[4].str(),
                .security_display = m[5].str(),
                .security_category = analysis::NormalizeSecurityCategory(m[5].str()),
                .channel = std::stoi(m[6].str()),
                .frequency_mhz = std::stoi(m[7].str()),
                .signal_dbm = std::stoi(m[8].str()),
                .hidden = false,
                .partial_observation = m[9].str() == "true",
                .confidence_score = std::stod(m[10].str())
            });
        }

        const std::regex issue_regex(R"(\{"rank":(-?\d+),"code":"([^"]*)","severity":"([^"]*)","title":"([^"]*)","description":"([^"]*)","evidence":"([^"]*)","fixSteps":"([^"]*)"\})");
        for (std::sregex_iterator it(report_json.begin(), report_json.end(), issue_regex), end; it != end; ++it)
        {
            const auto& m = *it;
            out_record.report.issues.push_back(analysis::AnalysisIssue{
                .code = m[2].str(),
                .severity = m[3].str(),
                .title = m[4].str(),
                .description = m[5].str(),
                .evidence = m[6].str(),
                .fix_steps = m[7].str(),
                .rank = std::stoi(m[1].str())
            });
        }

        return true;
    }

    std::vector<std::string> ListReportFiles(const std::string& root_directory)
    {
        std::vector<std::string> files;

        if (!std::filesystem::exists(root_directory))
        {
            return files;
        }

        for (const auto& entry : std::filesystem::directory_iterator(root_directory))
        {
            if (!entry.is_regular_file())
            {
                continue;
            }

            if (entry.path().extension() == ".json")
            {
                files.push_back(entry.path().string());
            }
        }

        std::sort(files.begin(), files.end());
        return files;
    }

    CompareReport CompareReports(const ReportRecord& left, const ReportRecord& right)
    {
        CompareReport compare{
            .left_report_id = left.id,
            .right_report_id = right.id
        };

        std::map<std::string, analysis::NormalizedAccessPoint> left_by_bssid;
        std::map<std::string, analysis::NormalizedAccessPoint> right_by_bssid;

        for (const auto& ap : left.report.access_points)
        {
            if (!ap.bssid.empty())
            {
                left_by_bssid[ap.bssid] = ap;
            }
        }

        for (const auto& ap : right.report.access_points)
        {
            if (!ap.bssid.empty())
            {
                right_by_bssid[ap.bssid] = ap;
            }
        }

        for (const auto& [bssid, ap] : left_by_bssid)
        {
            const auto it = right_by_bssid.find(bssid);
            if (it == right_by_bssid.end())
            {
                compare.deltas.push_back(AccessPointDelta{
                    .bssid = bssid,
                    .ssid = ap.ssid,
                    .change_type = "Removed",
                    .before_security = SecurityDisplay(ap),
                    .after_security = {},
                    .before_channel = ap.channel,
                    .after_channel = 0,
                    .before_signal_dbm = ap.signal_dbm,
                    .after_signal_dbm = 0,
                    .signal_delta_dbm = 0
                });
                continue;
            }

            const auto& newer = it->second;
            const auto security_changed = SecurityDisplay(ap) != SecurityDisplay(newer);
            const auto channel_changed = ap.channel != newer.channel;
            const auto signal_changed = ap.signal_dbm != newer.signal_dbm;

            if (security_changed || channel_changed || signal_changed)
            {
                compare.deltas.push_back(AccessPointDelta{
                    .bssid = bssid,
                    .ssid = newer.ssid.empty() ? ap.ssid : newer.ssid,
                    .change_type = "Changed",
                    .before_security = SecurityDisplay(ap),
                    .after_security = SecurityDisplay(newer),
                    .before_channel = ap.channel,
                    .after_channel = newer.channel,
                    .before_signal_dbm = ap.signal_dbm,
                    .after_signal_dbm = newer.signal_dbm,
                    .signal_delta_dbm = newer.signal_dbm - ap.signal_dbm
                });
            }
        }

        for (const auto& [bssid, ap] : right_by_bssid)
        {
            if (left_by_bssid.find(bssid) == left_by_bssid.end())
            {
                compare.deltas.push_back(AccessPointDelta{
                    .bssid = bssid,
                    .ssid = ap.ssid,
                    .change_type = "New",
                    .before_security = {},
                    .after_security = SecurityDisplay(ap),
                    .before_channel = 0,
                    .after_channel = ap.channel,
                    .before_signal_dbm = 0,
                    .after_signal_dbm = ap.signal_dbm,
                    .signal_delta_dbm = 0
                });
            }
        }

        std::stable_sort(compare.deltas.begin(), compare.deltas.end(), [](const AccessPointDelta& left_delta, const AccessPointDelta& right_delta)
        {
            if (left_delta.change_type != right_delta.change_type)
            {
                return left_delta.change_type < right_delta.change_type;
            }

            return left_delta.bssid < right_delta.bssid;
        });

        return compare;
    }

    bool ExportReportJson(const ReportRecord& record, const std::string& output_path)
    {
        return SaveReportRecordAsJson(record, output_path);
    }

    bool ExportReportCsv(const ReportRecord& record, const std::string& output_path)
    {
        std::ostringstream csv;
        csv << "SSID,BSSID,Vendor,Band,Security,Channel,FrequencyMhz,SignalDbm,ConfidenceScore\n";

        for (const auto& ap : record.report.access_points)
        {
            csv << Quote(ap.ssid) << ","
                << Quote(ap.bssid) << ","
                << Quote(ap.vendor) << ","
                << Quote(ap.band) << ","
                << Quote(ap.security_display) << ","
                << ap.channel << ","
                << ap.frequency_mhz << ","
                << ap.signal_dbm << ","
                << ap.confidence_score << "\n";
        }

        return WriteTextFile(output_path, csv.str());
    }
}