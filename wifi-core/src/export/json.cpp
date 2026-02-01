#include "wifi/export.hpp"

#include <algorithm>
#include <string>
#include <string_view>
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

std::string json_kv(std::string_view k, std::string_view v, bool quote_value = true) {
  using vyre::wifi::util::json_escape;
  std::string out;
  out += "\"";
  out += json_escape(k);
  out += "\":";
  if (quote_value) {
    out += "\"";
    out += json_escape(v);
    out += "\"";
  } else {
    out += v;
  }
  return out;
}

}

std::string to_json(const Report& report) {
  using vyre::wifi::util::json_escape;

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

  out += "{";

  out += json_kv("schema_version", std::to_string(report.schema_version), false) + ",";
  out += json_kv("report_id", report.report_id) + ",";
  out += json_kv("generated_at_utc", report.generated_at_utc) + ",";
  out += json_kv("platform", report.platform) + ",";
  out += json_kv("app_version", report.app_version) + ",";

  out += "\"scan\":{";
  out += json_kv("captured_at_utc", report.scan.captured_at_utc) + ",";
  out += "\"access_points\":[";
  for (std::size_t i = 0; i < aps.size(); ++i) {
    const auto& ap = aps[i];
    out += "{";
    out += json_kv("bssid", ap.bssid) + ",";
    out += json_kv("ssid", ap.ssid) + ",";
    out += json_kv("band", std::string(band_to_string(ap.band))) + ",";

    out += "\"channel\":";
    if (ap.channel.has_value()) out += std::to_string(*ap.channel);
    else out += "null";
    out += ",";
    out += "\"rssi_dbm\":";
    if (ap.rssi_dbm.has_value()) out += std::to_string(*ap.rssi_dbm);
    else out += "null";
    out += ",\"security\":";
    if (ap.security.has_value()) {
      out += "\"";
      out += json_escape(*ap.security);
      out += "\"";
    } else {
      out += "null";
    }

    out += "}";
    if (i + 1 < aps.size()) out += ",";
  }
  out += "]";
  out += "},";

  out += "\"issues\":[";
  for (std::size_t i = 0; i < issues.size(); ++i) {
    const auto& iss = issues[i];
    out += "{";
    out += json_kv("id", iss.id) + ",";
    out += json_kv("severity", std::string(severity_to_string(iss.severity))) + ",";
    out += json_kv("message", iss.message);
    out += "}";
    if (i + 1 < issues.size()) out += ",";
  }
  out += "],";

  out += "\"recommendations\":[";
  for (std::size_t i = 0; i < recs.size(); ++i) {
    const auto& r = recs[i];
    out += "{";
    out += json_kv("id", r.id) + ",";
    out += json_kv("priority", std::string(priority_to_string(r.priority))) + ",";
    out += json_kv("message", r.message);
    out += "}";
    if (i + 1 < recs.size()) out += ",";
  }
  out += "]";

  out += "}";

  return out;
}

}
