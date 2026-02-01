#define WIFI_INTEROP_BUILD
#include "wifi_api.h"

#include <algorithm>
#include <cctype>
#include <cstdlib>
#include <cstring>
#include <string>
#include <string_view>
#include <vector>

#include "wifi/export.hpp"
#include "wifi/report.hpp"
#include "wifi_core/wifi_core.h"

#ifdef _WIN32
#include "wifi/platform/windows/scanner_wlanapi.hpp"
#include "wifi/export.hpp"
#endif


namespace {

thread_local std::string g_last_error;

void set_last_error(std::string msg) {
  g_last_error = std::move(msg);
}

char* dup_utf8(const std::string& s) {
  const size_t n = s.size();
  char* p = static_cast<char*>(std::malloc(n + 1));
  if (!p) return nullptr;
  std::memcpy(p, s.data(), n);
  p[n] = '\0';
  return p;
}

bool is_ws(char c) {
  return std::isspace(static_cast<unsigned char>(c)) != 0;
}

struct JValue;

struct JObjectKV {
  std::string key;
  JValue* value;
};

struct JValue {
  enum class Type { Null, Bool, Number, String, Array, Object } type = Type::Null;

  bool b = false;
  double num = 0.0;
  std::string str;
  std::vector<JValue> arr;
  std::vector<std::pair<std::string, JValue>> obj;

  const JValue* get(std::string_view k) const {
    for (const auto& kv : obj) {
      if (kv.first == k) return &kv.second;
    }
    return nullptr;
  }
};

struct Parser {
  std::string_view s;
  size_t i = 0;

  explicit Parser(std::string_view input) : s(input) {}

  void skip_ws() {
    while (i < s.size() && is_ws(s[i])) ++i;
  }

  bool eof() const { return i >= s.size(); }

  bool consume(char c) {
    skip_ws();
    if (i < s.size() && s[i] == c) { ++i; return true; }
    return false;
  }

  bool expect(char c, const char* what) {
    skip_ws();
    if (i < s.size() && s[i] == c) { ++i; return true; }
    set_last_error(std::string("JSON parse error: expected ") + what);
    return false;
  }

  bool parse_hex_byte(char& out) {
    if (i + 2 > s.size()) return false;
    auto hex = [](char c) -> int {
      if (c >= '0' && c <= '9') return c - '0';
      if (c >= 'a' && c <= 'f') return 10 + (c - 'a');
      if (c >= 'A' && c <= 'F') return 10 + (c - 'A');
      return -1;
    };
    int hi = hex(s[i]);
    int lo = hex(s[i + 1]);
    if (hi < 0 || lo < 0) return false;
    out = static_cast<char>((hi << 4) | lo);
    i += 2;
    return true;
  }

  bool parse_string(std::string& out) {
    skip_ws();
    if (i >= s.size() || s[i] != '"') {
      set_last_error("JSON parse error: expected string");
      return false;
    }
    ++i;
    std::string result;
    while (i < s.size()) {
      char c = s[i++];
      if (c == '"') { out = std::move(result); return true; }
      if (c == '\\') {
        if (i >= s.size()) break;
        char e = s[i++];
        switch (e) {
          case '"': result.push_back('"'); break;
          case '\\': result.push_back('\\'); break;
          case '/': result.push_back('/'); break;
          case 'b': result.push_back('\b'); break;
          case 'f': result.push_back('\f'); break;
          case 'n': result.push_back('\n'); break;
          case 'r': result.push_back('\r'); break;
          case 't': result.push_back('\t'); break;
          case 'u': {
            if (i + 4 > s.size()) { set_last_error("JSON parse error: bad \\u escape"); return false; }
            char h1 = s[i], h2 = s[i+1], h3 = s[i+2], h4 = s[i+3];
            if (h1 != '0' || h2 != '0') { set_last_error("JSON parse error: only \\u00XX supported"); return false; }
            i += 2;
            char byte = 0;
            if (!parse_hex_byte(byte)) { set_last_error("JSON parse error: bad \\u00XX"); return false; }
            (void)h3; (void)h4;
            result.push_back(byte);
            break;
          }
          default:
            set_last_error("JSON parse error: unsupported escape");
            return false;
        }
      } else {
        result.push_back(c);
      }
    }
    set_last_error("JSON parse error: unterminated string");
    return false;
  }

  bool parse_number(double& out) {
    skip_ws();
    size_t start = i;
    if (i < s.size() && (s[i] == '-' || s[i] == '+')) ++i;
    bool any = false;
    while (i < s.size() && std::isdigit(static_cast<unsigned char>(s[i]))) { any = true; ++i; }
    if (i < s.size() && s[i] == '.') {
      ++i;
      while (i < s.size() && std::isdigit(static_cast<unsigned char>(s[i]))) { any = true; ++i; }
    }
    if (!any) {
      set_last_error("JSON parse error: expected number");
      return false;
    }
    std::string tmp(s.substr(start, i - start));
    out = std::strtod(tmp.c_str(), nullptr);
    return true;
  }

  bool parse_value(JValue& v) {
    skip_ws();
    if (i >= s.size()) { set_last_error("JSON parse error: unexpected EOF"); return false; }

    char c = s[i];
    if (c == 'n') {
      if (s.substr(i, 4) == "null") { i += 4; v.type = JValue::Type::Null; return true; }
    }
    if (c == 't') {
      if (s.substr(i, 4) == "true") { i += 4; v.type = JValue::Type::Bool; v.b = true; return true; }
    }
    if (c == 'f') {
      if (s.substr(i, 5) == "false") { i += 5; v.type = JValue::Type::Bool; v.b = false; return true; }
    }
    if (c == '"') {
      v.type = JValue::Type::String;
      return parse_string(v.str);
    }
    if (c == '{') {
      v.type = JValue::Type::Object;
      return parse_object(v);
    }
    if (c == '[') {
      v.type = JValue::Type::Array;
      return parse_array(v);
    }
    // number
    v.type = JValue::Type::Number;
    return parse_number(v.num);
  }

  bool parse_array(JValue& v) {
    if (!expect('[', "'['")) return false;
    skip_ws();
    if (consume(']')) return true;

    while (true) {
      JValue elem;
      if (!parse_value(elem)) return false;
      v.arr.push_back(std::move(elem));

      skip_ws();
      if (consume(']')) return true;
      if (!expect(',', "','")) return false;
    }
  }

  bool parse_object(JValue& v) {
    if (!expect('{', "'{'")) return false;
    skip_ws();
    if (consume('}')) return true;

    while (true) {
      std::string key;
      if (!parse_string(key)) return false;
      if (!expect(':', "':'")) return false;

      JValue val;
      if (!parse_value(val)) return false;
      v.obj.emplace_back(std::move(key), std::move(val));

      skip_ws();
      if (consume('}')) return true;
      if (!expect(',', "','")) return false;
    }
  }
};

bool require_string_field(const JValue& obj, std::string_view key, std::string& out) {
  const JValue* v = obj.get(key);
  if (!v || v->type != JValue::Type::String) {
    set_last_error(std::string("Missing or invalid string field: ") + std::string(key));
    return false;
  }
  out = v->str;
  return true;
}

bool optional_string_or_null(const JValue& obj, std::string_view key, std::optional<std::string>& out) {
  const JValue* v = obj.get(key);
  if (!v) { out.reset(); return true; }
  if (v->type == JValue::Type::Null) { out.reset(); return true; }
  if (v->type == JValue::Type::String) { out = v->str; return true; }
  set_last_error(std::string("Invalid field (expected string|null): ") + std::string(key));
  return false;
}

bool optional_int_or_null(const JValue& obj, std::string_view key, std::optional<int>& out) {
  const JValue* v = obj.get(key);
  if (!v) { out.reset(); return true; }
  if (v->type == JValue::Type::Null) { out.reset(); return true; }
  if (v->type == JValue::Type::Number) { out = static_cast<int>(v->num); return true; }
  set_last_error(std::string("Invalid field (expected number|null): ") + std::string(key));
  return false;
}

vyre::wifi::Band parse_band(std::string_view s) {
  using vyre::wifi::Band;
  if (s == "2.4ghz") return Band::GHz2_4;
  if (s == "5ghz") return Band::GHz5;
  if (s == "6ghz") return Band::GHz6;
  return Band::Unknown;
}

}

extern "C" {

const char* vyre_wifi_error_string(VyreWifiErrorCode code) {
  switch (code) {
    case VYRE_WIFI_OK: return "ok";
    case VYRE_WIFI_ERR_INVALID_ARGUMENT: return "invalid_argument";
    case VYRE_WIFI_ERR_PARSE_FAILED: return "parse_failed";
    case VYRE_WIFI_ERR_INTERNAL: return "internal_error";
    case VYRE_WIFI_ERR_NOT_IMPLEMENTED: return "not_implemented";
    default: return "unknown_error";
  }
}

const char* vyre_wifi_last_error_message(void) {
  return g_last_error.c_str();
}

void vyre_wifi_free(void* p) {
  std::free(p);
}

VyreWifiErrorCode vyre_wifi_get_version(char** out_utf8) {
  g_last_error.clear();
  if (!out_utf8) {
    set_last_error("out_utf8 is null");
    return VYRE_WIFI_ERR_INVALID_ARGUMENT;
  }
  *out_utf8 = nullptr;

  const char* ver = vyre::wifi::version();
  if (!ver) {
    set_last_error("wifi-core returned null version");
    return VYRE_WIFI_ERR_INTERNAL;
  }

  std::string s(ver);
  char* p = dup_utf8(s);
  if (!p) {
    set_last_error("malloc failed");
    return VYRE_WIFI_ERR_INTERNAL;
  }
  *out_utf8 = p;
  return VYRE_WIFI_OK;
}

VyreWifiErrorCode vyre_wifi_analyze_json(const char* scan_json_utf8, char** out_report_json_utf8) {
  g_last_error.clear();

  if (!scan_json_utf8 || !out_report_json_utf8) {
    set_last_error("scan_json_utf8 or out_report_json_utf8 is null");
    return VYRE_WIFI_ERR_INVALID_ARGUMENT;
  }
  *out_report_json_utf8 = nullptr;

  std::string_view input(scan_json_utf8);
  Parser p(input);
  JValue root;
  if (!p.parse_value(root) || root.type != JValue::Type::Object) {
    if (g_last_error.empty()) set_last_error("Invalid JSON root (expected object)");
    return VYRE_WIFI_ERR_PARSE_FAILED;
  }

  vyre::wifi::Report report;
  if (!require_string_field(root, "report_id", report.report_id)) return VYRE_WIFI_ERR_PARSE_FAILED;
  if (!require_string_field(root, "generated_at_utc", report.generated_at_utc)) return VYRE_WIFI_ERR_PARSE_FAILED;
  if (!require_string_field(root, "captured_at_utc", report.scan.captured_at_utc)) return VYRE_WIFI_ERR_PARSE_FAILED;
  if (!require_string_field(root, "platform", report.platform)) return VYRE_WIFI_ERR_PARSE_FAILED;
  if (!require_string_field(root, "app_version", report.app_version)) return VYRE_WIFI_ERR_PARSE_FAILED;

  const JValue* aps = root.get("access_points");
  if (!aps || aps->type != JValue::Type::Array) {
    set_last_error("Missing or invalid access_points (expected array)");
    return VYRE_WIFI_ERR_PARSE_FAILED;
  }

  report.scan.access_points.clear();
  report.scan.access_points.reserve(aps->arr.size());

  for (const auto& apv : aps->arr) {
    if (apv.type != JValue::Type::Object) {
      set_last_error("access_points element must be object");
      return VYRE_WIFI_ERR_PARSE_FAILED;
    }

    vyre::wifi::AccessPoint ap;
    if (!require_string_field(apv, "bssid", ap.bssid)) return VYRE_WIFI_ERR_PARSE_FAILED;
    if (!require_string_field(apv, "ssid", ap.ssid)) return VYRE_WIFI_ERR_PARSE_FAILED;

    std::string band;
    if (!require_string_field(apv, "band", band)) return VYRE_WIFI_ERR_PARSE_FAILED;
    ap.band = parse_band(band);

    if (!optional_int_or_null(apv, "channel", ap.channel)) return VYRE_WIFI_ERR_PARSE_FAILED;
    if (!optional_int_or_null(apv, "rssi_dbm", ap.rssi_dbm)) return VYRE_WIFI_ERR_PARSE_FAILED;

    if (!optional_string_or_null(apv, "security", ap.security)) return VYRE_WIFI_ERR_PARSE_FAILED;

    report.scan.access_points.push_back(std::move(ap));
  }

  report.issues.clear();
  report.recommendations.clear();

  bool weak = false;
  for (const auto& ap : report.scan.access_points) {
    if (ap.rssi_dbm && *ap.rssi_dbm <= -60) { weak = true; break; }
  }
  if (weak) {
    report.issues.push_back(vyre::wifi::Issue{
      .id = "weak_signal",
      .severity = vyre::wifi::IssueSeverity::Warning,
      .message = "Signal is weak on at least one access point."
    });
    report.recommendations.push_back(vyre::wifi::Recommendation{
      .id = "move_router",
      .priority = vyre::wifi::RecommendationPriority::High,
      .message = "Move router to a more central location."
    });
  }

  report.issues.push_back(vyre::wifi::Issue{
    .id = "report_generated",
    .severity = vyre::wifi::IssueSeverity::Info,
    .message = "Report generated successfully."
  });

  try {
    std::string out = vyre::wifi::exporter::to_json(report);
    char* p_out = dup_utf8(out);
    if (!p_out) {
      set_last_error("malloc failed");
      return VYRE_WIFI_ERR_INTERNAL;
    }
    *out_report_json_utf8 = p_out;
    return VYRE_WIFI_OK;
  } catch (...) {
    set_last_error("Exception while exporting report JSON");
    return VYRE_WIFI_ERR_INTERNAL;
  }
}

VyreWifiErrorCode vyre_wifi_scan_now(char** out_scan_json_utf8) {
  g_last_error.clear();

  if (!out_scan_json_utf8) {
    set_last_error("out_scan_json_utf8 is null");
    return VYRE_WIFI_ERR_INVALID_ARGUMENT;
  }
  *out_scan_json_utf8 = nullptr;

#ifndef _WIN32
  set_last_error("scan_now is only implemented on Windows in Milestone 4");
  return VYRE_WIFI_ERR_NOT_IMPLEMENTED;
#else
  try {
    vyre::wifi::WindowsWlanScanner scanner;
    auto scan = scanner.scan();
    std::string json = vyre::wifi::exporter::to_json(scan);

    char* p = dup_utf8(json);
    if (!p) {
      set_last_error("malloc failed");
      return VYRE_WIFI_ERR_INTERNAL;
    }

    *out_scan_json_utf8 = p;
    return VYRE_WIFI_OK;
  } catch (const vyre::wifi::ScanError& e) {
    set_last_error(e.what());
    return VYRE_WIFI_ERR_INTERNAL;
  } catch (const std::exception& e) {
    set_last_error(e.what());
    return VYRE_WIFI_ERR_INTERNAL;
  } catch (...) {
    set_last_error("Unknown exception in scan_now");
    return VYRE_WIFI_ERR_INTERNAL;
  }
#endif
}

VyreWifiErrorCode vyre_wifi_scan_start(void) {
  g_last_error.clear();
  set_last_error("scan_start not implemented in Milestone 2");
  return VYRE_WIFI_ERR_NOT_IMPLEMENTED;
}

VyreWifiErrorCode vyre_wifi_scan_stop(void) {
  g_last_error.clear();
  set_last_error("scan_stop not implemented in Milestone 2");
  return VYRE_WIFI_ERR_NOT_IMPLEMENTED;
}

VyreWifiErrorCode vyre_wifi_scan_get_results(char** out_scan_json_utf8) {
  g_last_error.clear();

  if (!out_scan_json_utf8) {
    set_last_error("out_scan_json_utf8 is null");
    return VYRE_WIFI_ERR_INVALID_ARGUMENT;
  }
  *out_scan_json_utf8 = nullptr;

#ifndef _WIN32
  set_last_error("scan_get_results is only implemented on Windows in Milestone 4");
  return VYRE_WIFI_ERR_NOT_IMPLEMENTED;
#else
  try {
    vyre::wifi::WindowsWlanScanner scanner;
    auto scan = scanner.scan();

    std::string json = vyre::wifi::exporter::to_json(scan);

    char* p = dup_utf8(json);
    if (!p) {
      set_last_error("malloc failed");
      return VYRE_WIFI_ERR_INTERNAL;
    }

    *out_scan_json_utf8 = p;
    return VYRE_WIFI_OK;
  } catch (const std::exception& e) {
    set_last_error(e.what());
    return VYRE_WIFI_ERR_INTERNAL;
  } catch (...) {
    set_last_error("Unknown exception in scan_get_results");
    return VYRE_WIFI_ERR_INTERNAL;
  }
#endif
}

}
