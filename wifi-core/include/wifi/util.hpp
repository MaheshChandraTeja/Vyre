#pragma once
#include <cctype>
#include <cstdint>
#include <optional>
#include <string>
#include <string_view>

namespace vyre::wifi::util {

inline std::string trim(std::string_view s) {
  std::size_t b = 0;
  while (b < s.size() && std::isspace(static_cast<unsigned char>(s[b]))) { ++b; }
  std::size_t e = s.size();
  while (e > b && std::isspace(static_cast<unsigned char>(s[e - 1]))) { --e; }
  return std::string(s.substr(b, e - b));
}


inline std::string json_escape(std::string_view s) {
  std::string out;
  out.reserve(s.size() + 8);
  for (char c : s) {
    switch (c) {
      case '\"': out += "\\\""; break;
      case '\\': out += "\\\\"; break;
      case '\b': out += "\\b"; break;
      case '\f': out += "\\f"; break;
      case '\n': out += "\\n"; break;
      case '\r': out += "\\r"; break;
      case '\t': out += "\\t"; break;
      default:

        if (static_cast<unsigned char>(c) < 0x20) {
          constexpr char hex[] = "0123456789abcdef";
          out += "\\u00";
          out += hex[(c >> 4) & 0xF];
          out += hex[c & 0xF];
        } else {
          out += c;
        }
    }
  }
  return out;
}

inline std::string csv_escape(std::string_view s) {

  bool needs = false;
  for (char c : s) {
    if (c == ',' || c == '"' || c == '\n' || c == '\r') { needs = true; break; }
  }
  if (!needs) return std::string(s);

  std::string out;
  out.reserve(s.size() + 8);
  out.push_back('"');
  for (char c : s) {
    if (c == '"') out += "\"\"";
    else out.push_back(c);
  }
  out.push_back('"');
  return out;
}

}
