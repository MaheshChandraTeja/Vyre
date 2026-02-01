#pragma once
#include <cstdint>
#include <string>
#include <string_view>

namespace vyre::wifi {

enum class RecommendationPriority : std::uint8_t { Low = 0, Medium, High };

struct Recommendation {
  std::string id;
  RecommendationPriority priority;
  std::string message;
};

inline std::string_view priority_to_string(RecommendationPriority p) {
  switch (p) {
    case RecommendationPriority::Low:    return "low";
    case RecommendationPriority::Medium: return "medium";
    case RecommendationPriority::High:   return "high";
    default:                             return "low";
  }
}

}
