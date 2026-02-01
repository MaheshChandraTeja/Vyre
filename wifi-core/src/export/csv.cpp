#include "wifi/export.hpp"

#include <algorithm>
#include <string>
#include <vector>

#include "wifi/util.hpp"

namespace vyre::wifi::exporter {

namespace {

template <typename T, typename KeyFn>
void stable_sort_by(std::vector<T>& v, KeyFn key) {
  std::stable_sort(v.begin(), v.end(), [&](const T& a, const T& b) {
    return key(a) < key(b);
  });
}

std::string csv_line(std::initializer_list<std::string> cols) {
  std::string out;
  bool first = true;
  for (const auto& c : cols) {
    if (!first) out += ",";
    first = false;
    out += c;
  }
  out += "\n";
  return out;
}

}

std::string to_csv(const Report& report) {
  using vyre::wifi::util::csv_escape;

  auto aps = report.scan.access_points;
  stable_sort_by(aps, [](const AccessPoint& ap) {
    return ap.ssid + "\n" + ap.bssid;
  });

  auto issues = report.issues;
  stable_sort_by(issues, [](const Issue& i) { return i.id; });

  auto recs = report.recommendations;
  stable_sort_by(recs, [](const Recommendation& r) { return r.id; });

  std::string out;
  out.reserve(2048);

  out += "# report\n";
  out += csv_line({"schema_version", std::to_string(report.schema_version)});
  out += csv_line({"report_id", csv_escape(report.report_id)});
  out += csv_line({"generated_at_utc", csv_escape(report.generated_at_utc)});
  out += csv_line({"platform", csv_escape(report.platform)});
  out += csv_line({"app_version", csv_escape(report.app_version)});
  out += "\n";

  out += "# scan\n";
  out += csv_line({"captured_at_utc", csv_escape(report.scan.captured_at_utc)});
  out += "\n";
  out += "# access_points\n";
  out += csv_line({"bssid", "ssid", "band", "channel", "rssi_dbm", "security"});
  for (const auto& ap : aps) {
    const std::string channel = ap.channel ? std::to_string(*ap.channel) : "";
    const std::string rssi = ap.rssi_dbm ? std::to_string(*ap.rssi_dbm) : "";
    const std::string sec = ap.security ? *ap.security : "";
    out += csv_line({
      csv_escape(ap.bssid),
      csv_escape(ap.ssid),
      csv_escape(std::string(band_to_string(ap.band))),
      csv_escape(channel),
      csv_escape(rssi),
      csv_escape(sec)
    });
  }
  out += "\n";

  out += "# issues\n";
  out += csv_line({"id", "severity", "message"});
  for (const auto& iss : issues) {
    out += csv_line({
      csv_escape(iss.id),
      csv_escape(std::string(severity_to_string(iss.severity))),
      csv_escape(iss.message)
    });
  }
  out += "\n";

  out += "# recommendations\n";
  out += csv_line({"id", "priority", "message"});
  for (const auto& r : recs) {
    out += csv_line({
      csv_escape(r.id),
      csv_escape(std::string(priority_to_string(r.priority))),
      csv_escape(r.message)
    });
  }

  return out;
}

}
