#pragma once
#include <cstdint>
#include <string>
#include <vector>

#include "wifi/issue.hpp"
#include "wifi/recommendation.hpp"
#include "wifi/scan_result.hpp"
#include "wifi/schema.hpp"

namespace vyre::wifi {

struct Report {
  std::uint32_t schema_version = schema::kReportSchemaVersion;

  std::string report_id;
  std::string generated_at_utc;
  std::string platform;
  std::string app_version;

  ScanResult scan;

  std::vector<Issue> issues;
  std::vector<Recommendation> recommendations;
};

}
