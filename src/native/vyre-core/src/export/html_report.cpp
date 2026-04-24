#include "vyre/core/reporting.hpp"

#include <fstream>
#include <sstream>

namespace vyre::core::reporting
{
    namespace
    {
        std::string EscapeHtml(const std::string_view value)
        {
            std::string escaped;
            escaped.reserve(value.size() + 8);

            for (const auto ch : value)
            {
                switch (ch)
                {
                case '&': escaped += "&amp;"; break;
                case '<': escaped += "&lt;"; break;
                case '>': escaped += "&gt;"; break;
                case '"': escaped += "&quot;"; break;
                case '\'': escaped += "&#39;"; break;
                default: escaped.push_back(ch); break;
                }
            }

            return escaped;
        }

        bool WriteTextFile(const std::string& output_path, const std::string& content)
        {
            std::ofstream out(output_path, std::ios::binary | std::ios::trunc);
            if (!out.is_open())
            {
                return false;
            }

            out.write(content.data(), static_cast<std::streamsize>(content.size()));
            return out.good();
        }
    }

    bool ExportReportHtml(const ReportRecord& record, const std::string& output_path)
    {
        std::ostringstream html;

        html << "<!doctype html><html><head><meta charset=\"utf-8\">"
             << "<title>Vyre Report</title>"
             << "<style>"
             << "body{font-family:Inter,Segoe UI,Arial,sans-serif;background:#0b1020;color:#e5e7eb;margin:0;padding:24px;}"
             << ".card{background:#111827;border:1px solid #1f2937;border-radius:16px;padding:16px;margin-bottom:16px;}"
             << "table{width:100%;border-collapse:collapse;font-size:14px;}"
             << "th,td{padding:10px;border-bottom:1px solid #1f2937;text-align:left;vertical-align:top;}"
             << "th{color:#93c5fd;font-weight:600;}"
             << ".muted{color:#94a3b8;}"
             << ".sev-High{color:#fca5a5;}.sev-Medium{color:#fcd34d;}.sev-Low,.sev-Info{color:#93c5fd;}"
             << "h1,h2,h3{margin:0 0 12px 0;}"
             << "</style></head><body>";

        html << "<div class=\"card\"><h1>Vyre Scan Report</h1>"
             << "<div class=\"muted\">Report ID: " << EscapeHtml(record.id) << "</div>"
             << "<div class=\"muted\">Source Platform: " << EscapeHtml(record.report.source_platform) << "</div>"
             << "<div class=\"muted\">Capability: " << EscapeHtml(record.report.capability_message) << "</div>"
             << "</div>";

        html << "<div class=\"card\"><h2>Access Points</h2><table><thead><tr>"
             << "<th>SSID</th><th>BSSID</th><th>Vendor</th><th>Band</th><th>Security</th><th>Channel</th><th>Signal</th><th>Confidence</th>"
             << "</tr></thead><tbody>";

        for (const auto& ap : record.report.access_points)
        {
            html << "<tr>"
                 << "<td>" << EscapeHtml(ap.ssid) << "</td>"
                 << "<td>" << EscapeHtml(ap.bssid) << "</td>"
                 << "<td>" << EscapeHtml(ap.vendor) << "</td>"
                 << "<td>" << EscapeHtml(ap.band) << "</td>"
                 << "<td>" << EscapeHtml(ap.security_display) << "</td>"
                 << "<td>" << ap.channel << "</td>"
                 << "<td>" << ap.signal_dbm << " dBm</td>"
                 << "<td>" << ap.confidence_score << "</td>"
                 << "</tr>";
        }

        html << "</tbody></table></div>";

        html << "<div class=\"card\"><h2>Insights</h2><table><thead><tr>"
             << "<th>Rank</th><th>Severity</th><th>Title</th><th>Description</th><th>Evidence</th><th>Fix Steps</th>"
             << "</tr></thead><tbody>";

        for (const auto& issue : record.report.issues)
        {
            html << "<tr>"
                 << "<td>" << issue.rank << "</td>"
                 << "<td class=\"sev-" << EscapeHtml(issue.severity) << "\">" << EscapeHtml(issue.severity) << "</td>"
                 << "<td>" << EscapeHtml(issue.title) << "</td>"
                 << "<td>" << EscapeHtml(issue.description) << "</td>"
                 << "<td>" << EscapeHtml(issue.evidence) << "</td>"
                 << "<td>" << EscapeHtml(issue.fix_steps) << "</td>"
                 << "</tr>";
        }

        html << "</tbody></table></div>";

        html << "</body></html>";

        return WriteTextFile(output_path, html.str());
    }
}