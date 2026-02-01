#pragma once
#include <string>

#include "wifi/report.hpp"

namespace vyre::wifi::exporter {

std::string to_json(const Report& report);
std::string to_csv(const Report& report);
}