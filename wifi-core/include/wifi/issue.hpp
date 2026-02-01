#pragma once
#include <cstdint>
#include <string>
#include <string_view>

namespace vyre::wifi {

enum class IssueSeverity : std::uint8_t { Info = 0, Warning, Critical };

struct Issue {
  std::string id;          // stable ID like "weak_signal"
  IssueSeverity severity;  // Info/Warning/Critical
  std::string message;     // human readable
};

inline std::string_view severity_to_string(IssueSeverity s) {
  switch (s) {
    case IssueSeverity::Info:     return "info";
    case IssueSeverity::Warning:  return "warning";
    case IssueSeverity::Critical: return "critical";
    default:                      return "info";
  }
}

} // namespace vyre::wifi
