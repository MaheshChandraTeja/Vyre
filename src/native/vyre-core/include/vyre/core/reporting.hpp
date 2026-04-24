#pragma once

#include "vyre/core/analysis.hpp"

#include <cstdint>
#include <string>
#include <vector>

namespace vyre::core::reporting
{
    struct ReportRecord
    {
        std::string id;
        std::int64_t captured_at_unix_utc = 0;
        analysis::AnalysisReport report;
    };

    struct AccessPointDelta
    {
        std::string bssid;
        std::string ssid;
        std::string change_type;
        std::string before_security;
        std::string after_security;
        int before_channel = 0;
        int after_channel = 0;
        int before_signal_dbm = 0;
        int after_signal_dbm = 0;
        int signal_delta_dbm = 0;
    };

    struct CompareReport
    {
        std::string left_report_id;
        std::string right_report_id;
        std::vector<AccessPointDelta> deltas;
    };

    bool SaveReportRecordAsJson(const ReportRecord& record, const std::string& output_path);
    bool LoadReportRecordFromJson(const std::string& input_path, ReportRecord& out_record);
    std::vector<std::string> ListReportFiles(const std::string& root_directory);

    CompareReport CompareReports(const ReportRecord& left, const ReportRecord& right);

    bool ExportReportJson(const ReportRecord& record, const std::string& output_path);
    bool ExportReportCsv(const ReportRecord& record, const std::string& output_path);
    bool ExportReportHtml(const ReportRecord& record, const std::string& output_path);
}